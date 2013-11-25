using System;
using System.Collections.Generic;
using System.Web.Mvc;
using FluentSecurity;

namespace TallyJ.Code.UnityRelated
{
  public class SecurityServiceLocator : ISecurityServiceLocator, ISecurityHandler
  {
    public object Resolve(Type typeToResolve)
    {
      return UnityInstance.Container.Resolve(typeToResolve,"");
    }

    public IEnumerable<object> ResolveAll(Type typeToResolve)
    {
      return UnityInstance.Container.ResolveAll(typeToResolve);
    }

    public ActionResult HandleSecurityFor(string controllerName, string actionName)
    {
      return (new RequireElectionPolicyViolationHandler().Handle(null));

    }

    public ActionResult HandleSecurityFor(string controllerName, string actionName, ISecurityContext securityContext)
    {
      throw new NotImplementedException();
    }
  }
}