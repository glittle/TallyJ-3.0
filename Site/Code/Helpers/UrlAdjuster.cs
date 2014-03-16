using System.Text.RegularExpressions;

namespace TallyJ.Code.Helpers
{
  /// <summary>
  /// Rewrites URLs
  /// </summary>
  public class UrlAdjuster
    {
        private readonly Regex _rCss = new Regex(@"^(.*\.css)(-\d*)$", RegexOptions.IgnoreCase);
        //private readonly Regex _rLess = new Regex(@"^(.*\.less)(-\d*)$", RegexOptions.IgnoreCase);
        private readonly Regex _rJs = new Regex(@"^(.*\.js)(-\d*)$", RegexOptions.IgnoreCase);

        private readonly string _url;
        private string _adjustedUrl;

        public UrlAdjuster(string url)
        {
            _adjustedUrl = "";
            _url = url;
        }

        public string AdjustedUrl
        {
            get
            {
              if (CheckPath(_rJs) || CheckPath(_rCss)) // || CheckPath(_rLess)
                {
                    return _adjustedUrl;
                }
                return null;
            }
        }

        private bool CheckPath(Regex re)
        {
            var match = re.Match(_url);
            if (match.Success)
            {
                var trailing = match.Groups[2].Value.Replace("/", "").AsInt();
                if (trailing != 0)
                {
                    _adjustedUrl = match.Groups[1].Value;
                    return true;
                }
            }
            return false;
        }
    }
}