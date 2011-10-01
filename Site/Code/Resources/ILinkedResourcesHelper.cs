namespace TallyJ.Code.Resources
{
	public interface ILinkedResourcesHelper
	{
		/// <summary>Create a link tag for this css file</summary>
		/// <param name="itemInfo">The name (without path) of the CSS file.  Can append media type with |. </param>
		/// <param name="rootPath">Folder for the CSS file. By default, this is the Content folder.</param>
		/// <example>Normal example:   main.css</example>
		/// <example>Example with media tag:   print.css|print</example>
		string CreateStyleSheetLinkTag(string itemInfo, string rootPath);

		/// <summary>Create a link tag for this css file</summary>
		/// <param name="itemInfo">The name (without path) of the CSS file.  Can append media type with |. </param>
		/// <example>Normal example:   main.css</example>
		/// <example>Example with media tag:   print.css|print</example>
		string CreateStyleSheetLinkTag(string itemInfo);

		/// <summary>Create a script tag for this js file</summary>
		/// <param name="name">The name (without path) of the js file. </param>
		/// <example>Normal example:   main.css</example>
		/// <example>Example with media tag:   print.css|print</example>
		string CreateJavascriptSourceTag(string name);

		/// <summary>Create a script tag for this js file</summary>
		/// <param name="name">The name (without path) of the js file. </param>
		/// <param name="rootPath">Folder for the js file. By default, this is the Scripts folder.</param>
		/// <example>Normal example:   main.css</example>
		/// <example>Example with media tag:   print.css|print</example>
		string CreateJavascriptSourceTag(string name, string rootPath);
	}
}