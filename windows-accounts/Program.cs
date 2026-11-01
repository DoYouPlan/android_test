using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Hryushki {
public static class Program {
 public static bool UiTesting;
 [STAThread]public static int Main(string[] args){
  if(args.Length==4&&args[0]=="--plugin-host")return PluginManager.Host(args);
  if(args.Length==2&&args[0]=="--plugin-test")return SelfTests.PluginIntegration(args[1]);
  if(args.Length>0&&args[0]=="--self-test")return SelfTests.Run(args.Length>1?args[1]:null);
  CultureInfo.DefaultThreadCurrentCulture=CultureInfo.GetCultureInfo("ru-RU");CultureInfo.DefaultThreadCurrentUICulture=CultureInfo.GetCultureInfo("ru-RU");
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  Application.ThreadException+=(s,e)=>MessageBox.Show(e.Exception.Message,"Ошибка операции",MessageBoxButtons.OK,MessageBoxIcon.Warning);
  try{
   string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HryushkiAccounts3");Directory.CreateDirectory(folder);
   string beside=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"hryushki_accounts_data.json");string remembered=Path.Combine(folder,"last_database.txt");
   string path=args.Length>0&&args[0]!="--ui-test"?Path.GetFullPath(args[0]):File.Exists(beside)?beside:File.Exists(remembered)&&File.Exists(File.ReadAllText(remembered).Trim())?File.ReadAllText(remembered).Trim():Path.Combine(folder,"hryushki_accounts_data.json");
   if(args.Length>0&&args[0]=="--ui-test"){
    UiTesting=true;using(var form=new MainWindow(args[1])){form.Show();Application.DoEvents();form.SmokePages();form.CaptureImage(args[2]);form.Close();}return 0;
   }
   Application.Run(new MainWindow(path));return 0;
  }catch(Exception e){if(UiTesting){Console.Error.WriteLine(e);File.WriteAllText(args[2]+".error.txt",e.ToString());return 1;}MessageBox.Show("Приложение не смогло открыть базу:\n"+e.GetBaseException().Message+"\n\nИсходный файл не заменён. Если другая копия программы открыта, закройте её.","Хрюшкины подушки",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
 }
}
public static class SelfTests {
 public static int PluginIntegration(string zip){string root=Path.Combine(Path.GetTempPath(),"HryushkiPluginTests_"+Guid.NewGuid().ToString("N"));try{var m=new PluginManager(root);var info=m.InspectPackage(zip);Check(info.Id=="hryushki.example.sales","plugin manifest");m.Install(zip);var p=m.List().Single();Check(!p.Enabled,"installed disabled");Check(Throws(()=>m.Run(p,J.Dump(J.Empty())).GetAwaiter().GetResult()),"disabled cannot run");m.Toggle(p);p=m.List().Single();Check(p.Enabled,"enabled setting persisted");var report=m.Run(p,J.Dump(J.Empty())).GetAwaiter().GetResult();Check(report.Title=="Продажи по складам","isolated child report");Check(Throws(()=>m.Install(zip)),"duplicate install rejected");m.Toggle(p);Check(!m.List().Single().Enabled,"disable plugin");m.Remove(p);Check(m.List().Count==0,"remove plugin");Console.WriteLine("Plugin integration passed");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{try{Directory.Delete(root,true);}catch{}}}
 static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);count++;Console.WriteLine("OK: "+name);}
 static Dictionary<string,object> Op(int id,string type,string item,decimal qty,decimal amount,string wh="Дома"){return new Dictionary<string,object>{{"id",id},{"date","01.11.2026"},{"type",type},{"item",item},{"qty",qty},{"amount",amount},{"warehouse",wh},{"category",""},{"note",""}};}
 static bool Throws(Action fn){try{fn();return false;}catch{return true;}}
 public static int Run(string legacy){string temp=Path.Combine(Path.GetTempPath(),"HryushkiTests_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);try{
  var d=J.Empty();var ops=new List<Dictionary<string,object>>{Op(1,"Корректировка склада","Тоннель",2,0),Op(2,"Передача партнёру","Тоннель",1,0,Engine.Partner),Op(3,"Продажа","Тоннель",1,550,Engine.Partner),Op(4,"Выплата партнёра","",0,500,Engine.Partner)};d["operations"]=ops;var t=Engine.Calculate(d);Check(t.Revenue==550&&t.Payouts==500&&t.Debt==50,"partner payout is not revenue");Check(t.Stock[Engine.Key(Engine.Home,"Тоннель")]==1&&t.Stock[Engine.Key(Engine.Partner,"Тоннель")]==0,"stock transfer and sale");
  ops[2]["fee"]=50;ops[2]["_cost"]=100;t=Engine.Calculate(d);Check(t.Result==400&&t.Debt==0,"fees, cost and debt");Check(Engine.Calculate(d,new DateTime(2026,12,1)).Revenue==0,"period revenue");Check(Engine.Calculate(d,null,new DateTime(2026,10,31)).Stock.Count==0,"historical balance");
  using(var store=new Store(Path.Combine(temp,"data.json"))){store.Save(d);Check(File.Exists(store.PathName),"atomic save");var snapshot=File.ReadAllText(store.PathName);Check(Throws(()=>store.ChangeOperation(Op(5,"Продажа","Тоннель",2,500))),"overselling rejected");Check(File.ReadAllText(store.PathName)==snapshot,"failed write leaves file intact");Check(Throws(()=>store.ChangeOperation(Op(5,"Выплата партнёра","",0,1,Engine.Partner))),"overpayment rejected");Check(Throws(()=>store.Void(3,"wrong")),"dependent cancellation rejected");
   store.ChangeOperation(Op(5,"Расход","Упаковка",0,20));Check(Directory.GetFiles(Path.Combine(temp,"backups")).Length>0,"backup before modification");store.Void(5,"duplicate");Check(J.Rows(store.Data,"operations").Count==5&&!Engine.Active(J.Rows(store.Data,"operations")[4]),"void audit retained");string export=Path.Combine(temp,"android.json");store.ExportAndroid(export);Check(J.Rows(J.Parse(File.ReadAllText(export)),"operations").Count==4,"Android excludes cancelled entries");File.AppendAllText(store.PathName," ");Check(Throws(()=>store.Save(store.Data)),"external writer conflict rejected");}
  var p=Path.Combine(temp,"bad.json");File.WriteAllText(p,"{broken");Check(Throws(()=>new Store(p)),"corrupt database not overwritten");Check(File.ReadAllText(p)=="{broken","original corrupt file preserved");
  d=J.Empty();d["materials"]=new[]{new Dictionary<string,object>{{"name","Флис"},{"qty",2}}};d["operations"]=new[]{Op(1,"Покупка материалов","Флис",3,900),Op(2,"Расход материалов","Флис",1,0)};t=Engine.Calculate(d);Check(t.Stock[Engine.Key("Материалы","Флис")]==4&&t.Expense==0&&t.CashFlow==-900,"material purchases are inventory cash flow");
  var r=new PluginReport{Title="Test",Columns=new[]{"a"},Rows=new List<string[]>{new[]{"b"}}};PluginManager.ValidateReport(r);Check(Throws(()=>PluginManager.ValidateReport(new PluginReport{Columns=new[]{"a"},Rows=new List<string[]>{new[]{"a","b"}}})),"plugin report shape validation");
  if(legacy!=null){var old=J.Parse(File.ReadAllText(legacy));J.Validate(old);t=Engine.Calculate(old);var expected=(Environment.GetEnvironmentVariable("HRYUSHKI_EXPECTED_TOTALS")??"").Split(';');Check(expected.Length==3&&t.Revenue==Decimal.Parse(expected[0],CultureInfo.InvariantCulture)&&t.Expense==Decimal.Parse(expected[1],CultureInfo.InvariantCulture)&&t.Debt==Decimal.Parse(expected[2],CultureInfo.InvariantCulture),"legacy 2.1 fixture totals");using(var s=new Store(Path.Combine(temp,"legacy.json"))){s.Save(old);s.ChangeOperation(Op(56,"Поступление","Тест",0,1));var roundtrip=J.Parse(File.ReadAllText(s.PathName));Check(J.Dump(old["products"])==J.Dump(roundtrip["products"]),"legacy catalog unchanged");Check(J.Dump(J.Rows(old,"operations")[0])==J.Dump(J.Rows(roundtrip,"operations")[0]),"legacy operations unchanged");}}
  Console.WriteLine("PASS "+count+" checks");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{try{Directory.Delete(temp,true);}catch{}}}
}
}
