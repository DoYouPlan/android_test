using System;
using System.Linq;
using System.Collections.Generic;
using Hryushki;
namespace Hryushki.Extensions {
public class StockPlannerPlugin:IAccountsPlugin {
 public PluginReport Execute(string snapshot){var s=Settings.Read("stock.settings.json",typeof(StockPlannerPlugin));return Build(J.Parse(snapshot),DateTime.Today,Settings.Days(s,"lookbackDays",60),Settings.Days(s,"targetDays",21),Settings.Minimum(s,"minimumHome",2),Settings.Minimum(s,"minimumPartner",1));}
 public static PluginReport Build(Dictionary<string,object> db,DateTime asOf,int lookback,int targetDays,decimal minimumHome,decimal minimumPartner){
  if(lookback<1||lookback>3650||targetDays<1||targetDays>3650||minimumHome<0||minimumPartner<0)throw new Exception("Некорректные настройки планирования");
  J.Validate(db);asOf=asOf.Date;var totals=Engine.Calculate(db,null,asOf);DateTime first=asOf.AddDays(1-lookback);
  var sales=J.Rows(db,"operations").Where(o=>Engine.Active(o)&&J.S(o,"type")=="Продажа"&&J.Date(J.S(o,"date"))>=first&&J.Date(J.S(o,"date"))<=asOf).ToList();
  var products=J.Rows(db,"products");var names=products.Select(p=>J.S(p,"name")).Concat(totals.Stock.Keys.Where(k=>k.StartsWith(Engine.Home+"\u001f")||k.StartsWith(Engine.Partner+"\u001f")).Select(k=>k.Substring(k.IndexOf('\u001f')+1))).Where(n=>!String.IsNullOrWhiteSpace(n)).Distinct().ToList();
  var report=new PluginReport{Title="Что сшить и передать партнёру",Columns=new[]{"Изделие","Дома","У партнёра","Продано / "+lookback+" дн.","Цель дома / партнёр","Передать, шт.","Сшить, шт.","Пояснение"},Rows=new List<string[]>()};
  var rows=new List<Tuple<int,decimal,string,string[]>>();int ignored=J.Rows(db,"operations").Count(o=>Engine.Active(o)&&J.S(o,"type")=="Продажа"&&J.S(o,"date")=="Без даты");
  foreach(string name in names){
   decimal home=0,partner=0;totals.Stock.TryGetValue(Engine.Key(Engine.Home,name),out home);totals.Stock.TryGetValue(Engine.Key(Engine.Partner,name),out partner);
   var sold=sales.Where(o=>J.S(o,"item")==name).ToList();decimal soldHome=sold.Where(o=>J.S(o,"warehouse")==Engine.Home).Sum(o=>J.N(o,"qty")),soldPartner=sold.Where(o=>J.S(o,"warehouse")==Engine.Partner).Sum(o=>J.N(o,"qty"));
   var card=products.FirstOrDefault(p=>J.S(p,"name")==name);
   bool offerHome=(card!=null&&J.N(card,"HomePrice")>0)||home!=0||soldHome>0;
   bool offerPartner=(card!=null&&J.N(card,"PartnerPrice")>0)||partner!=0||soldPartner>0;
   decimal homeTarget=offerHome?Math.Max(minimumHome,Decimal.Ceiling(soldHome/lookback*targetDays)):0;
   decimal partnerTarget=offerPartner?Math.Max(minimumPartner,Decimal.Ceiling(soldPartner/lookback*targetDays)):0;
   if(home<0||partner<0){rows.Add(Tuple.Create(0,0m,name,new[]{name,Settings.Quantity(home),Settings.Quantity(partner),Settings.Quantity(soldHome+soldPartner),"—","—","—","Сначала сверить отрицательные остатки"}));continue;}
   decimal partnerGap=Math.Max(0,partnerTarget-partner);decimal transfer=Math.Min(partnerGap,Math.Max(0,home-homeTarget));decimal sew=Math.Max(0,homeTarget-home)+partnerGap-transfer;
   string reason=!offerHome&&!offerPartner?"Не включено: нет цены, остатка и продаж":sold.Count==0?"Минимальный запас; спрос за период неизвестен":sew>0?"Пополнить запас на "+targetDays+" дней":transfer>0?"Есть запас дома для передачи":"Запаса достаточно";
   int priority=sew>0||transfer>0?1:2;rows.Add(Tuple.Create(priority,sew,name,new[]{name,Settings.Quantity(home),Settings.Quantity(partner),Settings.Quantity(soldHome+soldPartner),Settings.Quantity(homeTarget)+" / "+Settings.Quantity(partnerTarget),Settings.Quantity(transfer),Settings.Quantity(sew),reason}));
  }
  foreach(var r in rows.OrderBy(x=>x.Item1).ThenByDescending(x=>x.Item2).ThenBy(x=>x.Item3))report.Rows.Add(r.Item4);
  report.Summary="На "+asOf.ToString("dd.MM.yyyy")+". Спрос "+lookback+" дн., запас "+targetDays+" дн.; минимум "+Settings.Quantity(minimumHome)+" / "+Settings.Quantity(minimumPartner)+". Только Дома / Зверёк Аморе. Без даты вне спроса: "+ignored+". Сезонность и заказы не учтены. База не меняется.";
  return report;
 }
}
}
