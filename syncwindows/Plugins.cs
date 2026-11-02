using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hryushki {
// Stable extension API v1. The plugin receives a JSON snapshot, never a live Store.
public interface IAccountsPlugin { PluginReport Execute(string databaseJson); }
public class PluginReport {
 public string Title{get;set;}
 public string[] Columns{get;set;}
 public List<string[]> Rows{get;set;}
 public string Summary{get;set;}
}
public class PluginInfo {
 public string Id,Name,Version,Author,Description,AssemblyName,EntryType,Folder,Error="";
 public bool Enabled;
}
public class PluginManager {
 public string Root;string config;HashSet<string> enabled=new HashSet<string>();
 public PluginManager(string root){Root=root;Directory.CreateDirectory(root);config=Path.Combine(root,"enabled.json");if(File.Exists(config)){try{foreach(var id in J.Serializer().Deserialize<string[]>(File.ReadAllText(config)))enabled.Add(id);}catch{}}}
 static PluginInfo Manifest(string folder){
  string path=Path.Combine(folder,"plugin.json");if(new FileInfo(path).Length>32768)throw new Exception("Слишком большой манифест");var d=J.Parse(File.ReadAllText(path));
  var p=new PluginInfo{Id=J.S(d,"id"),Name=J.S(d,"name"),Version=J.S(d,"version"),Author=J.S(d,"author"),Description=J.S(d,"description"),AssemblyName=J.S(d,"assembly"),EntryType=J.S(d,"entryType"),Folder=folder};
  if(!Regex.IsMatch(p.Id,@"^[a-z][a-z0-9._-]{2,63}$")||p.Id.Contains(".."))throw new Exception("Неверный id плагина");
  if(J.N(d,"apiVersion")!=1)throw new Exception("Плагин требует другую версию API");
  if(String.IsNullOrWhiteSpace(p.Name)||String.IsNullOrWhiteSpace(p.Version)||String.IsNullOrWhiteSpace(p.EntryType))throw new Exception("Не заполнен манифест");
  if(Path.GetFileName(p.AssemblyName)!=p.AssemblyName||!p.AssemblyName.EndsWith(".dll",StringComparison.OrdinalIgnoreCase)||!File.Exists(Path.Combine(folder,p.AssemblyName)))throw new Exception("Не найдена DLL плагина");
  return p;
 }
 public List<PluginInfo> List(){var list=new List<PluginInfo>();foreach(var dir in Directory.GetDirectories(Root).Where(x=>!Path.GetFileName(x).StartsWith("_"))){try{var p=Manifest(dir);p.Enabled=enabled.Contains(p.Id);list.Add(p);}catch(Exception e){list.Add(new PluginInfo{Id=Path.GetFileName(dir),Name=Path.GetFileName(dir),Folder=dir,Error=e.Message});}}return list;}
 public void Toggle(PluginInfo p){if(p.Error!="")throw new Exception(p.Error);if(enabled.Contains(p.Id))enabled.Remove(p.Id);else enabled.Add(p.Id);WriteSettings();}
 void WriteSettings(){string temp=config+".tmp";File.WriteAllText(temp,J.Dump(enabled.ToArray()));if(File.Exists(config))File.Replace(temp,config,null);else File.Move(temp,config);}
 public PluginInfo InspectPackage(string zip){string temp=Path.Combine(Root,"_inspect_"+Guid.NewGuid().ToString("N"));try{Extract(zip,temp);return Manifest(temp);}finally{if(Directory.Exists(temp))Directory.Delete(temp,true);}}
 static void Extract(string zip,string dest){
  Directory.CreateDirectory(dest);long sum=0;using(var z=ZipFile.OpenRead(zip)){
   if(z.Entries.Count>100)throw new Exception("В пакете слишком много файлов");
   foreach(var e in z.Entries){string name=e.FullName.Replace('\\','/');if(name.EndsWith("/"))continue;
    if(name.Contains("/")||name.Contains(":")||name.Contains("..")||name.StartsWith(".")||!new[]{".json",".dll",".txt",".md"}.Contains(Path.GetExtension(name).ToLowerInvariant()))throw new Exception("Недопустимый файл плагина: "+name);
    if((e.ExternalAttributes>>16&0xF000)==0xA000)throw new Exception("Ссылки в пакете запрещены");
    sum+=e.Length;if(sum>10*1024*1024)throw new Exception("Пакет больше 10 МБ");
    using(var input=e.Open())using(var output=new FileStream(Path.Combine(dest,name),FileMode.CreateNew)){input.CopyTo(output);}
   }
  }
 }
 public void Install(string zip){
  string temp=Path.Combine(Root,"_install_"+Guid.NewGuid().ToString("N"));try{Extract(zip,temp);var p=Manifest(temp);string target=Path.Combine(Root,p.Id);if(Directory.Exists(target))throw new Exception("Плагин с таким id уже установлен. Сначала удалите старую версию.");Directory.Move(temp,target);enabled.Remove(p.Id);WriteSettings();}finally{if(Directory.Exists(temp))Directory.Delete(temp,true);}
 }
 public void Remove(PluginInfo p){enabled.Remove(p.Id);WriteSettings();Directory.Delete(p.Folder,true);}
 public async Task<PluginReport> Run(PluginInfo p,string snapshot){
  if(!enabled.Contains(p.Id))throw new Exception("Сначала включите плагин");
  string temp=Path.Combine(Path.GetTempPath(),"HryushkiPlugin_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  try{
   string input=Path.Combine(temp,"snapshot.json"),output=Path.Combine(temp,"report.json");File.WriteAllText(input,snapshot);
   var psi=new ProcessStartInfo{FileName=Assembly.GetExecutingAssembly().Location,Arguments="--plugin-host \""+p.Folder+"\" \""+input+"\" \""+output+"\"",UseShellExecute=false,CreateNoWindow=true};
   using(var child=Process.Start(psi)){bool exited=await Task.Run(()=>child.WaitForExit(15000));if(!exited){child.Kill();child.WaitForExit();throw new Exception("Плагин остановлен: превышено время 15 секунд");}if(!File.Exists(output)||new FileInfo(output).Length>1024*1024)throw new Exception("Плагин не вернул корректный отчёт");}
   var result=J.Parse(File.ReadAllText(output));if(result.ContainsKey("error"))throw new Exception(J.S(result,"error"));var report=J.Serializer().Deserialize<PluginReport>(J.Dump(result));ValidateReport(report);return report;
  }finally{try{Directory.Delete(temp,true);}catch{}}
 }
 public static void ValidateReport(PluginReport r){if(r==null||r.Columns==null||r.Columns.Length==0||r.Columns.Length>30||r.Rows==null||r.Rows.Count>10000||r.Rows.Any(row=>row==null||row.Length!=r.Columns.Length))throw new Exception("Плагин вернул неверную структуру отчёта");}
 public static int Host(string[] args){try{var p=Manifest(args[1]);Assembly assembly=Assembly.LoadFrom(Path.Combine(p.Folder,p.AssemblyName));Type t=assembly.GetType(p.EntryType,true);if(!typeof(IAccountsPlugin).IsAssignableFrom(t))throw new Exception("Класс не реализует API v1");var plugin=(IAccountsPlugin)Activator.CreateInstance(t);var report=plugin.Execute(File.ReadAllText(args[2]));ValidateReport(report);File.WriteAllText(args[3],J.Dump(report));return 0;}catch(Exception e){File.WriteAllText(args[3],J.Dump(new Dictionary<string,object>{{"error",e.GetBaseException().Message}}));return 1;}}
}
}
