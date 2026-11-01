using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Hryushki {
public static class Theme {
 public static Color Green=Color.FromArgb(42,91,66),Mint=Color.FromArgb(229,241,231),Paper=Color.FromArgb(246,248,243),Ink=Color.FromArgb(35,52,42),Muted=Color.FromArgb(112,126,117),Line=Color.FromArgb(227,233,225),Peach=Color.FromArgb(247,231,211),Red=Color.FromArgb(171,69,62);
 public static Font Font(float size=10, bool bold=false){return new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular);}
 public static Label Label(string text,float size=10,bool bold=false,Color? color=null){return new Label{Text=text,Font=Font(size,bold),ForeColor=color??Ink,AutoSize=false,TextAlign=ContentAlignment.MiddleLeft};}
 public static GraphicsPath Round(Rectangle r,int radius){int d=radius*2;var p=new GraphicsPath();p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public static Button Button(string text,Action action,bool primary=false){var b=new SoftButton{Text=text,BackColor=primary?Green:Color.White,ForeColor=primary?Color.White:Green,Height=40,Width=160,Font=Font(10,true),Margin=new Padding(0,0,10,0)};b.Click+=(s,e)=>action();return b;}
 public static DataGridView Grid(params string[] columns){var g=new DataGridView{Dock=DockStyle.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,ColumnHeadersHeight=44,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing,EnableHeadersVisualStyles=false,CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal,GridColor=Line,Font=Font(10)};
 g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(239,244,237),ForeColor=Muted,Font=Font(9,true),Padding=new Padding(10,0,0,0),SelectionBackColor=Mint};g.DefaultCellStyle=new DataGridViewCellStyle{ForeColor=Ink,BackColor=Color.White,SelectionBackColor=Mint,SelectionForeColor=Ink,Padding=new Padding(10,0,0,0)};g.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(250,251,248);g.RowTemplate.Height=42;
 foreach(string c in columns)g.Columns.Add(c,c);return g;}
}
public class SoftButton:Button {
 bool hover;public SoftButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Theme.Round(new Rectangle(1,1,Width-3,Height-3),9))using(var b=new SolidBrush(hover?ControlPaint.Light(BackColor,.08f):BackColor))e.Graphics.FillPath(b,p);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?ForeColor:Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-11,Height-11));}
}
public class Card:Panel {
 public Card(){DoubleBuffered=true;BackColor=Color.White;Padding=new Padding(18);}
 protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?Theme.Paper:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Theme.Round(new Rectangle(0,0,Width-1,Height-1),14))using(var b=new SolidBrush(Color.White))using(var pen=new Pen(Theme.Line)){e.Graphics.FillPath(b,p);e.Graphics.DrawPath(pen,p);}}
}
public class MainWindow:Form {
 Store store;PluginManager plugins;Panel body;Label heading,subtitle,status;DateTimePicker start,end;CheckBox all;TextBox search;string page="Обзор";Dictionary<string,Button> navigation=new Dictionary<string,Button>();string appRoot;DataGridView activeGrid;List<Dictionary<string,object>> shownOps;bool ready;
 public MainWindow(string path){
  store=new Store(path);appRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HryushkiAccounts3");Directory.CreateDirectory(appRoot);plugins=new PluginManager(Path.Combine(appRoot,"plugins"));
  Text="Хрюшкины подушки · Бухгалтерия 3.0";Font=Theme.Font();BackColor=Theme.Paper;ForeColor=Theme.Ink;Size=new Size(1380,900);MinimumSize=new Size(1050,720);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Font;
  var shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Theme.Paper};shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,230));shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(shell);
  var side=new Panel{Dock=DockStyle.Fill,BackColor=Theme.Green,Padding=new Padding(18,24,18,20)};shell.Controls.Add(side,0,0);
  var brand=Theme.Label("ХРЮШКИНЫ\nПОДУШКИ",18,true,Color.White);brand.SetBounds(22,70,190,74);side.Controls.Add(brand);
  var logo=new PigLogo{Location=new Point(24,21),Size=new Size(45,42)};side.Controls.Add(logo);
  var sub=Theme.Label("МАСТЕРСКАЯ · УЧЁТ",8,false,Color.FromArgb(190,215,196));sub.SetBounds(23,149,190,28);side.Controls.Add(sub);
  string[] pages={"Обзор","Операции","Склад изделий","Товары и цены","Материалы","Плагины","База и копии"};int yy=206;
  foreach(string p in pages){string key=p;var b=Theme.Button(p,()=>Navigate(key));b.TextAlign=ContentAlignment.MiddleLeft;b.BackColor=Theme.Green;b.ForeColor=Color.FromArgb(219,235,221);b.SetBounds(15,yy,200,46);side.Controls.Add(b);navigation[p]=b;yy+=53;}
  var offline=Theme.Label("●  Работает без интернета\nВерсия 3.0 · Windows",9,false,Color.FromArgb(190,215,196));offline.Dock=DockStyle.Bottom;offline.Height=60;side.Controls.Add(offline);
  var main=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(28,22,28,14),RowCount=4,ColumnCount=1};main.RowStyles.Add(new RowStyle(SizeType.Absolute,87));main.RowStyles.Add(new RowStyle(SizeType.Absolute,58));main.RowStyles.Add(new RowStyle(SizeType.Percent,100));main.RowStyles.Add(new RowStyle(SizeType.Absolute,35));shell.Controls.Add(main,1,0);
  var header=new Panel{Dock=DockStyle.Fill};heading=Theme.Label("Обзор",26,true);heading.SetBounds(0,0,580,46);heading.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;subtitle=Theme.Label("Мастерская в цифрах. Всё важное — под рукой.",10,false,Theme.Muted);subtitle.SetBounds(1,49,700,24);header.Controls.Add(heading);header.Controls.Add(subtitle);var add=Theme.Button("+  Новая операция",()=>EditOperation(),true);add.Width=180;add.Anchor=AnchorStyles.Top|AnchorStyles.Right;header.Controls.Add(add);header.Resize+=(s,e)=>add.Location=new Point(header.Width-add.Width,6);main.Controls.Add(header,0,0);
  var filters=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};all=new CheckBox{Text="За всё время",Checked=true,AutoSize=true,Margin=new Padding(0,10,10,0)};start=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd.MM.yyyy",Width=116,Value=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1),Margin=new Padding(0,7,8,0)};end=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd.MM.yyyy",Width=116,Value=DateTime.Today,Margin=new Padding(0,7,16,0)};search=new TextBox{Width=230,Margin=new Padding(0,7,0,0)};filters.Controls.Add(all);filters.Controls.Add(start);filters.Controls.Add(end);filters.Controls.Add(Theme.Label("Поиск",9,false,Theme.Muted));filters.Controls[3].Width=55;filters.Controls[3].Margin=new Padding(0,10,0,0);filters.Controls.Add(search);main.Controls.Add(filters,0,1);
  all.CheckedChanged+=(s,e)=>RefreshPage();start.ValueChanged+=(s,e)=>RefreshPage();end.ValueChanged+=(s,e)=>RefreshPage();search.TextChanged+=(s,e)=>RefreshPage();
  body=new Panel{Dock=DockStyle.Fill,BackColor=Theme.Paper};main.Controls.Add(body,0,2);status=Theme.Label("",8,false,Theme.Muted);status.Dock=DockStyle.Fill;status.AutoEllipsis=true;main.Controls.Add(status,0,3);
  FormClosed+=(s,e)=>store.Dispose();KeyPreview=true;KeyDown+=(s,e)=>{if(e.Control&&e.KeyCode==Keys.N){e.SuppressKeyPress=true;EditOperation();}if(e.KeyCode==Keys.F5)RefreshPage();};ready=true;Navigate("Обзор");
 }
 void Try(Action action){try{action();}catch(Exception e){if(Program.UiTesting)throw;MessageBox.Show(this,e.GetBaseException().Message,"Не удалось выполнить",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
 void Navigate(string p){page=p;search.Text="";RefreshPage();}
 bool Match(params string[] values){string q=search.Text.Trim();return q==""||values.Any(v=>(v??"").IndexOf(q,StringComparison.CurrentCultureIgnoreCase)>=0);}
 DateTime? From{get{return all.Checked?(DateTime?)null:start.Value.Date;}}DateTime? Until{get{return all.Checked?(DateTime?)null:end.Value.Date;}}
 public void RefreshPage(){if(!ready)return;Try(()=>{
  if(!all.Checked&&start.Value.Date>end.Value.Date)throw new Exception("Начало периода позже окончания");
  foreach(Control c in body.Controls.Cast<Control>().ToArray())c.Dispose();activeGrid=null;heading.Text=page;start.Enabled=end.Enabled=!all.Checked;
  foreach(var n in navigation){n.Value.BackColor=n.Key==page?Color.FromArgb(67,116,86):Theme.Green;n.Value.ForeColor=Color.White;}
  subtitle.Text=page=="Обзор"?"Мастерская в цифрах. Всё важное — под рукой.":page=="Плагины"?"Дополнительные отчёты и инструменты для вашей мастерской.":"Хрюшкины подушки / "+page;
  switch(page){case "Обзор":Overview();break;case "Операции":Operations();break;case "Склад изделий":Inventory();break;case "Товары и цены":Catalog(false);break;case "Материалы":Catalog(true);break;case "Плагины":PluginsPage();break;default:FilesPage();break;}
  status.Text="● Сохранение после каждого изменения    •    "+J.Rows(store.Data,"operations").Count+" записей    •    "+store.PathName;
 });}
 TableLayoutPanel PageLayout(int rows){var l=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=rows,ColumnCount=1};body.Controls.Add(l);return l;}
 FlowLayoutPanel Actions(params Button[] buttons){var f=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Height=50};foreach(var b in buttons)f.Controls.Add(b);return f;}
 Label Notice(string text,Color? color=null){var l=Theme.Label(text,9,false,color??Theme.Muted);l.Dock=DockStyle.Fill;l.Padding=new Padding(0,6,0,6);return l;}
 void Overview(){
  var t=Engine.Calculate(store.Data,From,Until);var l=PageLayout(4);l.RowStyles.Add(new RowStyle(SizeType.Absolute,145));l.RowStyles.Add(new RowStyle(SizeType.Absolute,74));l.RowStyles.Add(new RowStyle(SizeType.Percent,100));l.RowStyles.Add(new RowStyle(SizeType.Absolute,56));
  var cards=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1};for(int i=0;i<4;i++)cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));l.Controls.Add(cards,0,0);
  string[] titles={"ВЫРУЧКА","УЧТЁННЫЕ РАСХОДЫ","РЕЗУЛЬТАТ","ОЖИДАЕМ ОПЛАТУ"};decimal[] values={t.Revenue,t.Expense,t.Result,t.Debt};string[] notes={t.Sales+" продаж за период","Без закупок материалов","С учётом известной себестоимости","Накопительно на конец периода"};
  for(int i=0;i<4;i++){var c=new Card{Dock=DockStyle.Fill,Margin=new Padding(0,0,i==3?0:12,8)};var top=Theme.Label(titles[i],8,true,Theme.Muted);top.Dock=DockStyle.Top;top.Height=24;var v=Theme.Label(J.Money(values[i]),18,true,i==2&&values[i]<0?Theme.Red:Theme.Green);v.Dock=DockStyle.Top;v.Height=45;var n=Theme.Label(notes[i],8,false,Theme.Muted);n.Dock=DockStyle.Top;n.Height=28;c.Controls.Add(n);c.Controls.Add(v);c.Controls.Add(top);cards.Controls.Add(c,i,0);}
  string msg=t.UnknownCost>0?"Себестоимость не указана для "+t.UnknownCost+" продаж. Результат предварительный: он изменится после уточнения затрат.":"Себестоимость известных продаж учтена. Сумма на счетах зависит от начального остатка и полноты записей.";
  l.Controls.Add(Notice(msg,t.UnknownCost>0?Color.FromArgb(150,104,48):Theme.Muted),0,1);
  var lower=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));l.Controls.Add(lower,0,2);
  var recent=new Card{Dock=DockStyle.Fill,Margin=new Padding(0,0,14,0)};var g=Theme.Grid("Дата","Событие","Сумма");recent.Controls.Add(g);var rt=Theme.Label("Последние операции",13,true);rt.Dock=DockStyle.Top;rt.Height=42;recent.Controls.Add(rt);foreach(var o in FilterOps().Take(6))g.Rows.Add(J.S(o,"date"),J.S(o,"type")+" · "+J.S(o,"item"),J.Money(J.N(o,"amount")));lower.Controls.Add(recent,0,0);
  var health=new Card{Dock=DockStyle.Fill,Margin=new Padding(0)};var ht=Theme.Label("Нужно сверить",13,true);ht.Dock=DockStyle.Top;ht.Height=42;var info=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Color.White,ForeColor=Theme.Muted,Font=Theme.Font(10),ScrollBars=ScrollBars.Vertical};
  info.Text=t.Warnings.Count==0?"Ошибок остатков не обнаружено.\r\n\r\nРегулярно сверяйте остатки с фактическим наличием и сохраняйте копию базы на другой диск.":String.Join("\r\n\r\n",t.Warnings.Take(15));health.Controls.Add(info);health.Controls.Add(ht);lower.Controls.Add(health,1,0);
  l.Controls.Add(Notice("Чистое движение денег: "+J.Money(t.CashFlow)+"  •  это не остаток на карте без внесённого начального баланса."),0,3);
 }
 IEnumerable<Dictionary<string,object>> FilterOps(){return J.Rows(store.Data,"operations").Where(o=>(!From.HasValue||J.Date(J.S(o,"date"))>=From.Value)&&(!Until.HasValue||J.Date(J.S(o,"date"))<=Until.Value)&&Match(J.S(o,"item"),J.S(o,"category"),J.S(o,"note"),J.S(o,"type"),J.S(o,"warehouse"))).OrderByDescending(o=>J.Date(J.S(o,"date"))).ThenByDescending(o=>J.N(o,"id"));}
 void Operations(){var l=PageLayout(3);l.RowStyles.Add(new RowStyle(SizeType.Absolute,52));l.RowStyles.Add(new RowStyle(SizeType.Percent,100));l.RowStyles.Add(new RowStyle(SizeType.Absolute,36));l.Controls.Add(Actions(Theme.Button("Изменить",()=>EditSelected()),Theme.Button("Отменить запись",()=>VoidSelected()),Theme.Button("Экспорт CSV",()=>ExportGrid())),0,0);
  var g=Theme.Grid("№","Дата","Операция","Изделие / статья","Склад","Кол-во","Сумма","Статус");activeGrid=g;l.Controls.Add(g,0,1);g.Columns[0].FillWeight=35;g.Columns[5].FillWeight=45;shownOps=FilterOps().ToList();foreach(var o in shownOps){int r=g.Rows.Add(J.S(o,"id"),J.S(o,"date"),J.S(o,"type"),J.S(o,"item"),J.S(o,"warehouse"),J.N(o,"qty").ToString("0.###"),J.Money(J.N(o,"amount")),Engine.Active(o)?"Проведено":"Отменено");g.Rows[r].Tag=o;if(!Engine.Active(o))g.Rows[r].DefaultCellStyle.ForeColor=Theme.Muted;}g.DoubleClick+=(s,e)=>EditSelected();l.Controls.Add(Notice("Двойной щелчок — изменить. История правок и причины отмен хранятся в базе."),0,2);
 }
 Dictionary<string,object> Selected(){return activeGrid!=null&&activeGrid.SelectedRows.Count>0?activeGrid.SelectedRows[0].Tag as Dictionary<string,object>:null;}
 void EditSelected(){var o=Selected();if(o!=null&&Engine.Active(o))EditOperation(o);}
 void VoidSelected(){var o=Selected();if(o==null)return;string reason=Input("Причина отмены","Запись останется в журнале.","");if(reason==null)return;Try(()=>{store.Void((int)J.N(o,"id"),reason);RefreshPage();});}
 void Inventory(){var l=PageLayout(3);l.RowStyles.Add(new RowStyle(SizeType.Absolute,52));l.RowStyles.Add(new RowStyle(SizeType.Percent,100));l.RowStyles.Add(new RowStyle(SizeType.Absolute,38));l.Controls.Add(Actions(Theme.Button("Приход / корректировка",()=>EditOperation(null,"Корректировка склада")),Theme.Button("Передать партнёру",()=>EditOperation(null,"Передача партнёру")),Theme.Button("Экспорт CSV",()=>ExportGrid())),0,0);var g=Theme.Grid("Изделие","Склад","Остаток","Статус");activeGrid=g;l.Controls.Add(g,0,1);var t=Engine.Calculate(store.Data,null,Until);foreach(var p in t.Stock.OrderBy(x=>x.Key)){string[] k=p.Key.Split('\u001f');if(k[0]=="Материалы"||!Match(k)||p.Value==0)continue;int r=g.Rows.Add(k[1],k[0],p.Value.ToString("0.###"),p.Value<0?"Нужно сверить":"В наличии");if(p.Value<0)g.Rows[r].DefaultCellStyle.ForeColor=Theme.Red;}l.Controls.Add(Notice("Остатки рассчитаны на конец выбранного периода. Перемещение не создаёт выручку."),0,2);}
 void Catalog(bool material){
  var l=PageLayout(3);l.RowStyles.Add(new RowStyle(SizeType.Absolute,52));l.RowStyles.Add(new RowStyle(SizeType.Percent,100));l.RowStyles.Add(new RowStyle(SizeType.Absolute,43));
  var buttons=new List<Button>{Theme.Button(material?"+ Материал":"+ Изделие",()=>EditCatalog(material,null),true),Theme.Button("Изменить",()=>{var o=Selected();if(o!=null)EditCatalog(material,o);}),Theme.Button("Экспорт CSV",()=>ExportGrid())};if(material)buttons.Add(Theme.Button("Закупка / расход",()=>EditOperation(null,"Покупка материалов")));l.Controls.Add(Actions(buttons.ToArray()),0,0);
  var g=material?Theme.Grid("Материал","Единица","Начальный остаток","Текущий остаток","Цена за единицу"):Theme.Grid("Изделие","Цена дома","Цена партнёру","Себестоимость за шт.");activeGrid=g;l.Controls.Add(g,0,1);var stock=Engine.Calculate(store.Data,null,Until).Stock;
  foreach(var p in J.Rows(store.Data,material?"materials":"products").OrderBy(x=>J.S(x,"name"))){if(!Match(J.S(p,"name")))continue;int r;if(material){decimal current;stock.TryGetValue(Engine.Key("Материалы",J.S(p,"name")),out current);r=g.Rows.Add(J.S(p,"name"),J.S(p,"unit","м"),J.N(p,"qty").ToString("0.###"),current.ToString("0.###"),J.Money(J.N(p,"price")));}else r=g.Rows.Add(J.S(p,"name"),J.Money(J.N(p,"HomePrice")),J.Money(J.N(p,"PartnerPrice")),J.N(p,"Cost")>0?J.Money(J.N(p,"Cost")):"Не задана");g.Rows[r].Tag=p;}
  g.DoubleClick+=(s,e)=>{var o=Selected();if(o!=null)EditCatalog(material,o);};l.Controls.Add(Notice(material?"Количество в карточке — начальный остаток. Дальнейшее движение проводите операциями закупки и расхода.":"Себестоимость подставляется в новые продажи и сохраняется в них. Старые продажи требуют отдельного уточнения."),0,2);
 }
 void EditCatalog(bool material,Dictionary<string,object> original){Try(()=>{
  using(var f=new EditorDialog(material?"Материал":"Карточка изделия")){
   var n=original==null?new Dictionary<string,object>():J.Copy(original);var name=f.TextField("Название",J.S(n,"name"));
   TextBox unit=null;NumericUpDown q=null,a=null,b=null,c=null;
   if(material){unit=f.TextField("Единица измерения",J.S(n,"unit","м"));q=f.Number("Начальный остаток",J.N(n,"qty"),3);a=f.Number("Цена за единицу, ₽",J.N(n,"price"),2);}
   else{a=f.Number("Цена дома, ₽",J.N(n,"HomePrice"),2);b=f.Number("Цена партнёру, ₽",J.N(n,"PartnerPrice"),2);c=f.Number("Себестоимость за единицу, ₽",J.N(n,"Cost"),2);}
   f.Help(material?"Начальный остаток — до операций закупки и списания.":"Укажите полную себестоимость: материалы, упаковка и оценка труда. Исторические продажи автоматически не пересчитываются.");
   f.SaveAction=()=>{if(String.IsNullOrWhiteSpace(name.Text))throw new Exception("Введите название");var db=J.Copy(store.Data);string key=material?"materials":"products";var list=J.Rows(db,key);if(list.Any(x=>J.S(x,"name")==name.Text.Trim()&&(original==null||J.S(x,"name")!=J.S(original,"name"))))throw new Exception("Название уже существует");
    if(original!=null&&J.S(original,"name")!=name.Text.Trim()&&J.Rows(db,"operations").Any(x=>J.S(x,"item")==J.S(original,"name")))throw new Exception("У товара уже есть операции. Создайте новую карточку, чтобы сохранить историю названия.");
    n["name"]=name.Text.Trim();if(material){n["unit"]=unit.Text.Trim();n["qty"]=q.Value;n["price"]=a.Value;}else{n["HomePrice"]=a.Value;n["PartnerPrice"]=b.Value;n["Cost"]=c.Value;}
    if(original==null)list.Add(n);else list[list.FindIndex(x=>J.S(x,"name")==J.S(original,"name"))]=n;db[key]=list;Engine.ValidateChange(store.Data,db);store.Save(db);};if(Program.UiTesting){f.Show(this);Application.DoEvents();f.Close();}else if(f.ShowDialog(this)==DialogResult.OK)RefreshPage();
  }
 });}
 void EditOperation(Dictionary<string,object> original=null,string initial=null){Try(()=>{
  using(var f=new EditorDialog(original==null?"Новая операция":"Изменение операции №"+J.S(original,"id"))){
   var o=original==null?new Dictionary<string,object>():J.Copy(original);
   var type=f.Choice("Что произошло",Engine.Types,initial??J.S(o,"type","Продажа"),false);
   var date=f.DateField("Дата",original==null||J.S(o,"date")=="Без даты"?DateTime.Today:J.Date(J.S(o,"date")));
   var wh=f.Choice("Склад / партнёр",Engine.Warehouses(store.Data),J.S(o,"warehouse",Engine.Home),false);
   string[] names=J.Rows(store.Data,"products").Select(x=>J.S(x,"name")).Concat(J.Rows(store.Data,"materials").Select(x=>J.S(x,"name"))).Distinct().OrderBy(x=>x).ToArray();
   var item=f.Choice("Изделие, материал или статья",names,J.S(o,"item"),true);
   var q=f.Number("Количество",J.N(o,"qty",1),3,true);var amount=f.Number("Общая сумма за всё количество, ₽",J.N(o,"amount"),2);
   var fee=f.Number("Удержанная комиссия, ₽",J.N(o,"fee"),2);var paid=f.Number("Фактически получено по продаже, ₽",J.N(o,"paid",J.S(o,"warehouse",Engine.Home)==Engine.Home?J.N(o,"amount")-J.N(o,"fee"):0),2);
   var who=f.TextField("Покупатель / партнёр, который должен оплатить",J.S(o,"counterparty"));
   var known=f.Check("Себестоимость этой продажи известна",o.ContainsKey("_cost"));var cost=f.Number("Себестоимость всего проданного количества, ₽",J.N(o,"_cost"),2);
   var cat=f.TextField("Категория / канал",J.S(o,"category"));var note=f.TextField("Комментарий / номер заказа",J.S(o,"note"));
   Action update=()=>{bool sale=type.Text=="Продажа";f.FieldVisible(fee,sale);f.FieldVisible(paid,sale);f.FieldVisible(who,sale||type.Text=="Выплата партнёра");f.FieldVisible(known,sale);f.FieldVisible(cost,sale&&known.Checked);};
   Action suggest=()=>{if(original!=null)return;var p=J.Rows(store.Data,"products").FirstOrDefault(x=>J.S(x,"name")==item.Text);if(type.Text=="Продажа"&&p!=null){amount.Value=Math.Min(amount.Maximum,J.N(p,wh.Text==Engine.Home?"HomePrice":"PartnerPrice")*Math.Max(0,q.Value));known.Checked=J.N(p,"Cost")>0;cost.Value=Math.Min(cost.Maximum,J.N(p,"Cost")*Math.Max(0,q.Value));}if(type.Text=="Продажа")paid.Value=wh.Text==Engine.Home?Math.Max(0,amount.Value-fee.Value):0;};
   type.SelectedIndexChanged+=(s,e)=>{update();suggest();};wh.SelectedIndexChanged+=(s,e)=>suggest();item.SelectedIndexChanged+=(s,e)=>suggest();q.ValueChanged+=(s,e)=>suggest();known.CheckedChanged+=(s,e)=>update();
   f.Help("Продажа: сумма до комиссии. Полученная оплата уменьшает долг. Выплата партнёра погашает долг и не создаёт вторую выручку. Закупка материалов влияет на движение денег; себестоимость учитывается при продаже. Новые операции не могут ухудшить нехватку остатков.");update();
   f.SaveAction=()=>{o["date"]=date.Value.ToString("dd.MM.yyyy");o["type"]=type.Text;o["warehouse"]=wh.Text;o["item"]=item.Text.Trim();o["qty"]=q.Value;o["amount"]=amount.Value;o["category"]=cat.Text.Trim();o["note"]=note.Text.Trim();
    if(type.Text=="Продажа"){o["fee"]=fee.Value;o["paid"]=paid.Value;o["counterparty"]=String.IsNullOrWhiteSpace(who.Text)?wh.Text:who.Text.Trim();if(known.Checked)o["_cost"]=cost.Value;else o.Remove("_cost");}
    else{o.Remove("fee");o.Remove("paid");o.Remove("_cost");o["counterparty"]=String.IsNullOrWhiteSpace(who.Text)?wh.Text:who.Text.Trim();}
    if((type.Text=="Покупка материалов"||type.Text=="Расход материалов")&&!J.Rows(store.Data,"materials").Any(m=>J.S(m,"name")==item.Text.Trim()))throw new Exception("Сначала добавьте материал в разделе «Материалы»");
    store.ChangeOperation(o,original==null?0:(int)J.N(original,"id"));};
   if(Program.UiTesting){f.Show(this);Application.DoEvents();f.Close();}else if(f.ShowDialog(this)==DialogResult.OK)RefreshPage();
  }
 });}
 void PluginsPage(){
  var l=PageLayout(3);l.RowStyles.Add(new RowStyle(SizeType.Absolute,54));l.RowStyles.Add(new RowStyle(SizeType.Percent,100));l.RowStyles.Add(new RowStyle(SizeType.Absolute,80));
  var g=Theme.Grid("Дополнение","Версия","Автор","Состояние");activeGrid=g;
  Func<PluginInfo> selected=()=>g.SelectedRows.Count==0?null:g.SelectedRows[0].Tag as PluginInfo;
  var run=Theme.Button("Запустить",()=>{} ,true);run.Click+=async(s,e)=>{var p=selected();if(p==null)return;run.Enabled=false;try{var report=await plugins.Run(p,J.Dump(store.Data));using(var window=new ReportWindow(report))window.ShowDialog(this);}catch(Exception ex){MessageBox.Show(this,ex.GetBaseException().Message,"Плагин остановлен",MessageBoxButtons.OK,MessageBoxIcon.Information);}finally{if(!run.IsDisposed)run.Enabled=true;}};
  l.Controls.Add(Actions(Theme.Button("+ Установить ZIP",()=>Try(()=>{using(var d=new OpenFileDialog{Filter="Пакет плагина|*.zip"}){if(d.ShowDialog(this)!=DialogResult.OK)return;var p=plugins.InspectPackage(d.FileName);if(MessageBox.Show(this,p.Name+" · "+p.Version+"\nАвтор: "+p.Author+"\n\n"+p.Description+"\n\nПлагин выполняет доверенный .NET-код с правами вашего пользователя. При запуске он получает копию всей базы. Устанавливайте дополнения знакомого автора.\n\nУстановить?", "Установка дополнения",MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes)return;plugins.Install(d.FileName);RefreshPage();}})),Theme.Button("Включить / выключить",()=>Try(()=>{var p=selected();if(p!=null){plugins.Toggle(p);RefreshPage();}})),run,Theme.Button("Удалить",()=>Try(()=>{var p=selected();if(p!=null&&MessageBox.Show(this,"Удалить дополнение «"+p.Name+"»?","Плагины",MessageBoxButtons.YesNo)==DialogResult.Yes){plugins.Remove(p);RefreshPage();}}))),0,0);
  foreach(var p in plugins.List()){int r=g.Rows.Add(p.Name,p.Version,p.Author,p.Error!=""?p.Error:p.Enabled?"Включён":"Выключен");g.Rows[r].Tag=p;}
  l.Controls.Add(g,0,1);l.Controls.Add(Notice("API v1: отчёты по снимку базы, без изменения операций через API. Установленный плагин сначала выключен. Каждый запуск — в отдельном процессе с ограничением 15 секунд.\nЭто изоляция сбоев, а не защита от вредоносного кода. Набор разработчика и пример лежат в архиве программы."),0,2);
 }
 void FilesPage(){
  var l=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};body.Controls.Add(l);
  Action<string,string,string,Action> tile=(title,detail,button,action)=>{var c=new Card{Width=Math.Max(700,body.Width-25),Height=112,Margin=new Padding(0,0,0,12)};var h=Theme.Label(title,13,true);h.SetBounds(18,12,c.Width-210,30);var n=Theme.Label(detail,9,false,Theme.Muted);n.SetBounds(18,43,c.Width-210,52);n.AutoEllipsis=true;var b=Theme.Button(button,action);b.SetBounds(c.Width-177,32,155,42);c.Controls.Add(h);c.Controls.Add(n);c.Controls.Add(b);l.Controls.Add(c);};
  tile("Открыть существующую базу","JSON из версии 2.1 или 3.0. Файл останется на выбранном месте.","Открыть JSON",()=>Try(()=>{using(var d=new OpenFileDialog{Filter="База учёта|*.json"}){if(d.ShowDialog(this)==DialogResult.OK&&Path.GetFullPath(d.FileName)!=store.PathName){store.Open(d.FileName);Remember();RefreshPage();}}}));
  tile("Резервная копия","Копия всей базы с историей правок. Сохраняйте её на другой диск.","Сохранить копию",()=>Try(()=>{string p=SavePath("Резервная копия|*.json","accounts_backup_"+DateTime.Now.ToString("yyyyMMdd_HHmm")+".json");if(p!=null){File.WriteAllText(p,J.Dump(store.Data),new UTF8Encoding(false));MessageBox.Show(this,"Копия сохранена.");}}));
  tile("Восстановить из копии","Текущая база будет заменена. Перед заменой создаётся автоматическая копия.","Восстановить",()=>Try(()=>{using(var d=new OpenFileDialog{Filter="База учёта|*.json"}){if(d.ShowDialog(this)!=DialogResult.OK)return;var next=J.Parse(File.ReadAllText(d.FileName));J.Validate(next);if(MessageBox.Show(this,"Заменить текущую базу содержимым копии?","Восстановление",MessageBoxButtons.YesNo)==DialogResult.Yes){store.Save(next);RefreshPage();}}}));
  tile("Копия для Android 1.0","Исключает отменённые записи. Старый Android-клиент не умеет учитывать частичную оплату и себестоимость.","Экспорт JSON",()=>Try(()=>{string p=SavePath("База JSON|*.json","hryushki_accounts_data.json");if(p!=null)store.ExportAndroid(p);}));
  tile("Папка автоматических копий","Перед изменением базы сохраняется копия. Хранятся последние 30 состояний.","Открыть папку",()=>Try(()=>{string dir=Path.Combine(Path.GetDirectoryName(store.PathName),"backups");Directory.CreateDirectory(dir);System.Diagnostics.Process.Start("explorer.exe",dir);}));
  tile("О программе","Версия 3.0 · Локальный управленческий учёт\nОперации, склады, материалы, цены, история изменений, плагины.","Инструкция",()=>MessageBox.Show(this,"Начните с открытия JSON-базы версии 2.1 либо внесите остатки.\nCtrl+N — новая операция. F5 — обновить.\n\nВыручка и выплаты разделены. Себестоимость сохраняется в каждой новой продаже; нулевая цена в карточке означает, что её надо уточнить.\nЗакупки материалов — движение денег, их стоимость включайте в себестоимость изделия.\n\nАвтокопии сохраняются рядом с выбранной базой в backups. Инструкция и API плагинов — в архиве.","Хрюшкины подушки 3.0"));
 }
 void Remember(){File.WriteAllText(Path.Combine(appRoot,"last_database.txt"),store.PathName);}
 string SavePath(string filter,string filename){using(var d=new SaveFileDialog{Filter=filter,FileName=filename,AddExtension=true,OverwritePrompt=true}){if(d.ShowDialog(this)!=DialogResult.OK)return null;if(Path.GetFullPath(d.FileName).Equals(store.PathName,StringComparison.OrdinalIgnoreCase))throw new Exception("Выберите другое имя: это текущая база");return d.FileName;}}
 void ExportGrid(){Try(()=>{if(activeGrid==null)return;string path=SavePath("Таблица CSV|*.csv","hryushki_"+DateTime.Today.ToString("yyyyMMdd")+".csv");if(path==null)return;var rows=new List<string[]>();rows.Add(activeGrid.Columns.Cast<DataGridViewColumn>().Select(c=>c.HeaderText).ToArray());rows.AddRange(activeGrid.Rows.Cast<DataGridViewRow>().Select(r=>r.Cells.Cast<DataGridViewCell>().Select(c=>Convert.ToString(c.Value)).ToArray()));Csv.Write(path,rows);MessageBox.Show(this,"Таблица сохранена. Экспорт учитывает поиск и выбранный период.");});}
 static string Input(string title,string hint,string value){using(var f=new EditorDialog(title)){var t=f.TextField(hint,value);f.SaveAction=()=>{if(String.IsNullOrWhiteSpace(t.Text))throw new Exception("Введите значение");};return f.ShowDialog()==DialogResult.OK?t.Text:null;}}
 public void CaptureImage(string path){using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(path);}}
 public void SmokePages(){foreach(string p in navigation.Keys.ToArray()){Navigate(p);Application.DoEvents();}EditOperation();EditCatalog(false,null);EditCatalog(true,null);Navigate("Обзор");}
}
public class PigLogo:Control {
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var b=new SolidBrush(Theme.Peach)){g.FillEllipse(b,2,4,16,18);g.FillEllipse(b,27,4,16,18);g.FillEllipse(b,4,9,37,29);}using(var b=new SolidBrush(Theme.Green)){g.FillEllipse(b,13,18,4,4);g.FillEllipse(b,29,18,4,4);}using(var b=new SolidBrush(Color.FromArgb(215,169,137)))g.FillEllipse(b,18,25,12,8);}
}
public class EditorDialog:Form {
 FlowLayoutPanel fields;Dictionary<Control,Panel> parents=new Dictionary<Control,Panel>();public Action SaveAction;
 public EditorDialog(string title){Text=title;Font=Theme.Font();BackColor=Theme.Paper;Width=650;Height=790;MinimumSize=new Size(530,500);StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.Font;
  var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=70,Padding=new Padding(22,12,0,10),BackColor=Color.White};var save=Theme.Button("Сохранить",()=>{try{if(SaveAction!=null)SaveAction();DialogResult=DialogResult.OK;Close();}catch(Exception e){MessageBox.Show(this,e.GetBaseException().Message,"Проверьте данные",MessageBoxButtons.OK,MessageBoxIcon.Warning);}},true);var cancel=Theme.Button("Отмена",()=>{DialogResult=DialogResult.Cancel;Close();});footer.Controls.Add(save);footer.Controls.Add(cancel);AcceptButton=save;CancelButton=cancel;
  fields=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(24,16,20,10)};Controls.Add(fields);Controls.Add(footer);
 }
 void Add(string label,Control c){var row=new Panel{Width=550,Height=70,Margin=new Padding(0,0,0,7)};var l=Theme.Label(label,9,true,Theme.Muted);l.SetBounds(0,0,545,23);c.SetBounds(0,28,545,30);row.Controls.Add(l);row.Controls.Add(c);fields.Controls.Add(row);parents[c]=row;}
 public TextBox TextField(string label,string text){var c=new TextBox{Text=text,Font=Theme.Font(11)};Add(label,c);return c;}
 public ComboBox Choice(string label,string[] choices,string value,bool editable){var c=new ComboBox{DropDownStyle=editable?ComboBoxStyle.DropDown:ComboBoxStyle.DropDownList,Font=Theme.Font(11),IntegralHeight=false,MaxDropDownItems=12};c.Items.AddRange(choices);if(!choices.Contains(value)&&!editable&&value!="")c.Items.Add(value);c.Text=value;Add(label,c);return c;}
 public NumericUpDown Number(string label,decimal value,int places,bool negative=false){var c=new NumericUpDown{DecimalPlaces=places,Maximum=1000000000,Minimum=negative?-1000000000:0,ThousandsSeparator=true,Font=Theme.Font(11)};c.Value=Math.Max(c.Minimum,Math.Min(c.Maximum,value));Add(label,c);return c;}
 public DateTimePicker DateField(string label,DateTime value){var c=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd.MM.yyyy",Value=value};Add(label,c);return c;}
 public CheckBox Check(string label,bool value){var c=new CheckBox{Text=label,Checked=value};Add("Себестоимость",c);return c;}
 public void FieldVisible(Control c,bool visible){parents[c].Visible=visible;}
 public void Help(string text){var l=Theme.Label(text,9,false,Theme.Muted);l.Size=new Size(545,115);l.Margin=new Padding(0,12,0,10);fields.Controls.Add(l);}
}
public class ReportWindow:Form {
 public ReportWindow(PluginReport r){Text=r.Title;Size=new Size(900,650);StartPosition=FormStartPosition.CenterParent;Font=Theme.Font();var g=Theme.Grid(r.Columns);foreach(var row in r.Rows)g.Rows.Add(row.Cast<object>().ToArray());Controls.Add(g);var header=Theme.Label(r.Title+"\n"+r.Summary,13,true);header.Dock=DockStyle.Top;header.Height=85;header.Padding=new Padding(16);Controls.Add(header);var b=Theme.Button("Экспорт CSV",()=>{using(var d=new SaveFileDialog{Filter="CSV|*.csv",FileName="plugin_report.csv"}){if(d.ShowDialog(this)==DialogResult.OK)Csv.Write(d.FileName,new[]{r.Columns}.Concat(r.Rows));}});b.Dock=DockStyle.Bottom;b.Height=48;Controls.Add(b);}
}
public static class Csv {
 public static void Write(string path,IEnumerable<string[]> rows){using(var f=new StreamWriter(path,false,new UTF8Encoding(true))){foreach(var row in rows)f.WriteLine(String.Join(";",row.Select(Escape)));}}
 static string Escape(string s){s=s??"";if(s.Length>0&&"=+-@\t\r\n".Contains(s[0]))s="'"+s;return "\""+s.Replace("\"","\"\"")+"\"";}
}
}
