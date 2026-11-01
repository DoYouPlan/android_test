using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Hryushki;
namespace Hryushki.Extensions {
internal static class Settings {
 public static Dictionary<string,object> Read(string name,Type owner){string path=Path.Combine(Path.GetDirectoryName(owner.Assembly.Location),name);if(!File.Exists(path))return new Dictionary<string,object>();if(new FileInfo(path).Length>32768)throw new Exception("Файл настроек слишком большой");return J.Parse(File.ReadAllText(path));}
 public static int Days(Dictionary<string,object> d,string key,int value){decimal v=J.N(d,key,value);if(v<1||v>3650||v!=Decimal.Truncate(v))throw new Exception(key+": требуется целое число от 1 до 3650");return (int)v;}
 public static decimal Minimum(Dictionary<string,object> d,string key,int value){decimal v=J.N(d,key,value);if(v<0||v>10000)throw new Exception(key+": требуется число от 0 до 10000");return v;}
 public static string Quantity(decimal n){return n.ToString("0.###",System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));}
}
}
