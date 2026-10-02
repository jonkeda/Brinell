using System.Text;

namespace Brinell.Generator.Writers;

public class ImportDictionary
{
	private Dictionary<string, string> Imports = new Dictionary<string, string>();

	public void Add(string import)
	{
		if (!Imports.ContainsKey(import))
		{
			Imports.Add(import, import);
		}
	}

	public string ToString(string aPackage)
	{
		var sb = new StringBuilder();

		foreach (string key in Imports.Keys.Where(i =>
				         !i.StartsWith("java.") &&
				         !i.StartsWith("javax.") &&
				         !i.StartsWith(aPackage))
			         .OrderBy(i => i).ToList())
		{
			sb.AppendFormat("using {0};\n", key);
		}

		foreach (string key in Imports.Keys.Where(i => i.StartsWith("javax.")).OrderBy(i => i).ToList())
		{
			sb.AppendFormat("using {0};\n", key);
		}

		foreach (string key in Imports.Keys.Where(i => i.StartsWith("java.")).OrderBy(i => i).ToList())
		{
			sb.AppendFormat("using {0};\n", key);
		}

		return sb.ToString();
	}
}