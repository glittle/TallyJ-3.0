using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  /// <summary>
  /// One critical section for an attempt-limit bucket.
  /// Threads in this process take a lock. When the context is the real database, the same
  /// section also takes a SQL Server application lock so two worker processes cannot both
  /// pass a check before either records the miss. A timeout fails closed.
  /// </summary>
  public static class AttemptGate
  {
    public const string BusyMessage = "Too many attempts. Please wait before trying again.";

    private static readonly ConcurrentDictionary<string, object> Gates = new ConcurrentDictionary<string, object>();

    [ThreadStatic]
    private static HashSet<string> _heldKeys;

    public static void Run(ITallyJDbContext db, string key, Action action)
    {
      Run(db, key, () =>
      {
        action();
        return 0;
      });
    }

    public static T Run<T>(ITallyJDbContext db, string key, Func<T> action)
    {
      key = Fit(key);
      if (_heldKeys != null && _heldKeys.Contains(key))
      {
        return action();
      }

      var gate = Gates.GetOrAdd(key, _ => new object());
      lock (gate)
      {
        if (_heldKeys == null)
        {
          _heldKeys = new HashSet<string>(StringComparer.Ordinal);
        }

        _heldKeys.Add(key);
        try
        {
          if (db is TallyJEntities entities)
          {
            return WithSqlLock(entities, key, action);
          }

          return action();
        }
        finally
        {
          _heldKeys.Remove(key);
        }
      }
    }

    private static T WithSqlLock<T>(TallyJEntities entities, string key, Func<T> action)
    {
      DbContextTransaction transaction = null;
      try
      {
        transaction = entities.Database.BeginTransaction();
        int code;
        try
        {
          code = Acquire(entities, key);
        }
        catch (Exception)
        {
          // The login may not be allowed to run sp_getapplock. This process is already serialized.
          transaction.Dispose();
          transaction = null;
          return action();
        }

        if (code < 0)
        {
          transaction.Dispose();
          transaction = null;
          throw new AttemptGateDeniedException();
        }

        var result = action();
        transaction.Commit();
        return result;
      }
      finally
      {
        transaction?.Dispose();
      }
    }

    private static int Acquire(TallyJEntities entities, string key)
    {
      var command = entities.Database.Connection.CreateCommand();
      command.Transaction = entities.Database.CurrentTransaction.UnderlyingTransaction;
      command.CommandText =
        "DECLARE @result int; " +
        "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; " +
        "SELECT @result;";
      var parameter = command.CreateParameter();
      parameter.ParameterName = "@resource";
      parameter.Value = key;
      command.Parameters.Add(parameter);
      using (command)
      {
        return Convert.ToInt32(command.ExecuteScalar());
      }
    }

    private static string Fit(string key)
    {
      if (string.IsNullOrEmpty(key))
      {
        return "tallyj:empty";
      }

      if (key.Length <= 255)
      {
        return key;
      }

      using (var sha = SHA256.Create())
      {
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        return "tallyj:" + Convert.ToBase64String(hash);
      }
    }
  }

  public class AttemptGateDeniedException : Exception
  {
  }
}
