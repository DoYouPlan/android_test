package ru.hryushki.accounts;

import android.app.*;
import android.os.Bundle;
import android.content.*;
import android.graphics.Color;
import android.graphics.Typeface;
import android.net.Uri;
import android.view.*;
import android.widget.*;
import org.json.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.*;

public class MainActivity extends Activity {
    private static final int OPEN_DB = 101;
    private static final int EXPORT_DB = 102;
    private static final int GREEN = Color.rgb(47,107,79);
    private static final int DARK = Color.rgb(36,52,43);
    private static final int MINT = Color.rgb(234,245,238);

    private LinearLayout body;
    private TextView status;
    private JSONObject db;
    private Uri currentUri;

    @Override public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        buildShell();
        loadDemoEmpty();
        showOverview();
    }

    private void buildShell() {
        LinearLayout root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setBackgroundColor(MINT);

        LinearLayout header = new LinearLayout(this);
        header.setOrientation(LinearLayout.VERTICAL);
        header.setPadding(dp(18), dp(18), dp(18), dp(14));
        header.setBackgroundColor(GREEN);

        TextView title = text("🐹  Хрюшкины подушки", 22, Color.WHITE, true);
        TextView sub = text("Бухгалтерия • Android", 14, Color.rgb(230,245,235), false);
        status = text("База не загружена", 12, Color.rgb(220,240,228), false);
        status.setPadding(0, dp(6), 0, 0);
        header.addView(title); header.addView(sub); header.addView(status);
        root.addView(header);

        HorizontalScrollView navScroll = new HorizontalScrollView(this);
        navScroll.setHorizontalScrollBarEnabled(false);
        LinearLayout nav = new LinearLayout(this);
        nav.setPadding(dp(10),dp(8),dp(10),dp(8));
        nav.setOrientation(LinearLayout.HORIZONTAL);
        nav.setBackgroundColor(Color.WHITE);
        nav.addView(navButton("Обзор", v->showOverview()));
        nav.addView(navButton("Операции", v->showOperations()));
        nav.addView(navButton("Склад", v->showStock()));
        nav.addView(navButton("+ Операция", v->showAddOperation()));
        nav.addView(navButton("Файл", v->showFileMenu()));
        navScroll.addView(nav);
        root.addView(navScroll);

        ScrollView sv = new ScrollView(this);
        body = new LinearLayout(this);
        body.setOrientation(LinearLayout.VERTICAL);
        body.setPadding(dp(14), dp(14), dp(14), dp(30));
        sv.addView(body);
        root.addView(sv, new LinearLayout.LayoutParams(-1,0,1));
        setContentView(root);
    }

    private Button navButton(String s, View.OnClickListener l) {
        Button b = new Button(this); b.setText(s); b.setTextSize(13); b.setAllCaps(false);
        b.setTextColor(GREEN); b.setBackgroundColor(Color.TRANSPARENT); b.setOnClickListener(l);
        b.setPadding(dp(10),0,dp(10),0); return b;
    }

    private Button primary(String s, View.OnClickListener l) {
        Button b = new Button(this); b.setText(s); b.setAllCaps(false); b.setTextColor(Color.WHITE);
        b.setTextSize(15); b.setBackgroundResource(R.drawable.button_bg);
        b.setOnClickListener(l); b.setPadding(dp(14),dp(10),dp(14),dp(10));
        LinearLayout.LayoutParams p = new LinearLayout.LayoutParams(-1, dp(52)); p.setMargins(0,dp(7),0,dp(7)); b.setLayoutParams(p); return b;
    }

    private TextView text(String s, int sp, int color, boolean bold) {
        TextView t = new TextView(this); t.setText(s); t.setTextSize(sp); t.setTextColor(color);
        if (bold) t.setTypeface(Typeface.DEFAULT, Typeface.BOLD); return t;
    }

    private LinearLayout card(String title, String value, String note) {
        LinearLayout c = new LinearLayout(this); c.setOrientation(LinearLayout.VERTICAL); c.setPadding(dp(16),dp(14),dp(16),dp(14));
        c.setBackgroundResource(R.drawable.card_bg);
        TextView a=text(title,13,DARK,true); TextView b=text(value,25,GREEN,true); TextView n=text(note,12,Color.DKGRAY,false);
        b.setPadding(0,dp(5),0,dp(4)); c.addView(a); c.addView(b); c.addView(n);
        LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(-1,-2); p.setMargins(0,0,0,dp(10)); c.setLayoutParams(p); return c;
    }

    private void clear(String heading) {
        body.removeAllViews(); TextView h=text(heading,21,DARK,true); h.setPadding(0,0,0,dp(12)); body.addView(h);
    }

    private void loadDemoEmpty() {
        try {
            db=new JSONObject(); db.put("version",2); db.put("warehouses", new JSONArray(Arrays.asList("Дома","Зверёк Аморе")));
            db.put("operations", new JSONArray()); db.put("products", new JSONArray()); db.put("materials", new JSONArray());
        } catch(Exception ignored) {}
    }

    private JSONArray ops(){ return db.optJSONArray("operations")!=null?db.optJSONArray("operations"):new JSONArray(); }
    private JSONArray products(){ return db.optJSONArray("products")!=null?db.optJSONArray("products"):new JSONArray(); }

    private void showOverview() {
        clear("Обзор");
        double income=0, expense=0, partner=0, payouts=0; int saleCount=0;
        JSONArray a=ops();
        for(int i=0;i<a.length();i++){
            JSONObject o=a.optJSONObject(i); if(o==null) continue; String type=o.optString("type"); double amount=o.optDouble("amount",0);
            if(type.equals("Продажа")||type.equals("Поступление")){income+=amount; if(type.equals("Продажа")) saleCount++;}
            if(type.equals("Расход")) expense+=amount;
            if(type.equals("Продажа") && o.optString("warehouse").equals("Зверёк Аморе")) partner+=amount;
            if(type.toLowerCase().contains("выплат") || (type.equals("Поступление") && o.optString("category").toLowerCase().contains("партн"))) payouts+=amount;
        }
        body.addView(card("Доходы", money(income), saleCount+" продаж в базе"));
        body.addView(card("Расходы", money(expense), "Учтённые денежные расходы"));
        body.addView(card("Результат", money(income-expense), "Доходы минус расходы"));
        body.addView(card("Долг партнёра", money(partner-payouts), "Продажи «Зверёк Аморе» минус выплаты"));
        body.addView(card("Операций", String.valueOf(a.length()), currentUri==null?"Загрузите базу из файла":"База загружена с телефона"));
        body.addView(primary("Загрузить базу JSON", v->openDatabase()));
    }

    private void showOperations() {
        clear("Операции"); JSONArray a=ops();
        if(a.length()==0){ body.addView(text("Операций пока нет. Загрузите базу или добавьте первую запись.",14,Color.DKGRAY,false)); return; }
        for(int i=a.length()-1;i>=0;i--){
            JSONObject o=a.optJSONObject(i); if(o==null) continue;
            LinearLayout c=new LinearLayout(this); c.setOrientation(LinearLayout.VERTICAL); c.setPadding(dp(14),dp(12),dp(14),dp(12)); c.setBackgroundResource(R.drawable.card_bg);
            String line=o.optString("type")+" • "+o.optString("date");
            String item=o.optString("item"); int qty=o.optInt("qty",0); double amount=o.optDouble("amount",0);
            c.addView(text(line,13,GREEN,true)); c.addView(text(item+(qty!=0?" × "+qty:"")+ (amount!=0?"   "+money(amount):""),16,DARK,true));
            c.addView(text(o.optString("warehouse")+ (o.optString("category").isEmpty()?"":" • "+o.optString("category")),12,Color.DKGRAY,false));
            LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(-1,-2);p.setMargins(0,0,0,dp(8));c.setLayoutParams(p);body.addView(c);
        }
    }

    private void showStock() {
        clear("Склад");
        Map<String,Integer> home=new TreeMap<>(), partner=new TreeMap<>();
        JSONArray a=ops();
        for(int i=0;i<a.length();i++){
            JSONObject o=a.optJSONObject(i); if(o==null) continue;
            String type=o.optString("type"), item=o.optString("item"), wh=o.optString("warehouse"); int q=o.optInt("qty",0);
            if(item.isEmpty()||q==0) continue;
            if(type.equals("Передача партнёру")){ addQty(home,item,-q); addQty(partner,item,q); }
            else if(type.equals("Продажа")){ addQty(wh.equals("Зверёк Аморе")?partner:home,item,-q); }
            else if(type.equals("Производство")||type.equals("Корректировка склада")){ addQty(wh.equals("Зверёк Аморе")?partner:home,item,q); }
        }
        addStockSection("🏠 Дома",home); addStockSection("🤝 Зверёк Аморе",partner);
    }

    private void addQty(Map<String,Integer> m,String k,int q){ m.put(k,m.getOrDefault(k,0)+q); }
    private void addStockSection(String name, Map<String,Integer> m){
        TextView h=text(name,17,GREEN,true); h.setPadding(0,dp(8),0,dp(8)); body.addView(h);
        if(m.isEmpty()){body.addView(text("Нет данных",13,Color.DKGRAY,false));return;}
        for(Map.Entry<String,Integer> e:m.entrySet()){
            LinearLayout row=new LinearLayout(this); row.setPadding(dp(14),dp(10),dp(14),dp(10));row.setBackgroundResource(R.drawable.card_bg);
            TextView n=text(e.getKey(),14,DARK,false); TextView q=text(String.valueOf(e.getValue()),16,e.getValue()<0?Color.RED:GREEN,true);
            row.addView(n,new LinearLayout.LayoutParams(0,-2,1));row.addView(q);LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(-1,-2);p.setMargins(0,0,0,dp(6));row.setLayoutParams(p);body.addView(row);
        }
    }

    private void showAddOperation(){
        clear("Новая операция");
        Spinner type=new Spinner(this); String[] types={"Продажа","Расход","Поступление","Производство","Передача партнёру","Корректировка склада","Выплата партнёра"}; type.setAdapter(new ArrayAdapter<>(this,android.R.layout.simple_spinner_dropdown_item,types));
        Spinner wh=new Spinner(this); wh.setAdapter(new ArrayAdapter<>(this,android.R.layout.simple_spinner_dropdown_item,new String[]{"Дома","Зверёк Аморе"}));
        Spinner item=new Spinner(this); List<String> names=new ArrayList<>(); JSONArray ps=products(); for(int i=0;i<ps.length();i++){JSONObject p=ps.optJSONObject(i); if(p!=null) names.add(p.optString("name"));} if(names.isEmpty()) names.add("Другое"); item.setAdapter(new ArrayAdapter<>(this,android.R.layout.simple_spinner_dropdown_item,names));
        EditText qty=input("Количество", "1"); qty.setInputType(2); EditText amount=input("Сумма, ₽",""); amount.setInputType(8194); EditText cat=input("Категория",""); EditText note=input("Примечание","");
        body.addView(label("Тип операции"));body.addView(type);body.addView(label("Склад"));body.addView(wh);body.addView(label("Изделие / статья"));body.addView(item);body.addView(qty);body.addView(amount);body.addView(cat);body.addView(note);
        body.addView(primary("Добавить и сохранить",v->{
            try{
                JSONObject o=new JSONObject(); o.put("id",nextId()); o.put("date",new SimpleDateFormat("dd.MM.yyyy",Locale.getDefault()).format(new Date())); o.put("type",type.getSelectedItem().toString()); o.put("warehouse",wh.getSelectedItem().toString()); o.put("item",item.getSelectedItem().toString());
                o.put("qty",parseInt(qty.getText().toString(),1)); o.put("amount",parseDouble(amount.getText().toString())); o.put("category",cat.getText().toString().trim()); o.put("note",note.getText().toString().trim()); ops().put(o);
                toast("Операция добавлена"); if(currentUri!=null) saveToUri(currentUri); showOverview();
            }catch(Exception e){ toast("Не удалось добавить: "+e.getMessage()); }
        }));
    }

    private TextView label(String s){TextView t=text(s,12,GREEN,true);t.setPadding(0,dp(8),0,0);return t;}
    private EditText input(String hint,String val){EditText e=new EditText(this);e.setHint(hint);e.setText(val);e.setTextColor(DARK);e.setHintTextColor(Color.GRAY);e.setPadding(0,dp(10),0,dp(10));return e;}
    private int nextId(){int max=0;JSONArray a=ops();for(int i=0;i<a.length();i++)max=Math.max(max,a.optJSONObject(i)!=null?a.optJSONObject(i).optInt("id",0):0);return max+1;}
    private int parseInt(String s,int d){try{return Integer.parseInt(s.trim());}catch(Exception e){return d;}}
    private double parseDouble(String s){try{return Double.parseDouble(s.trim().replace(',','.'));}catch(Exception e){return 0;}}

    private void showFileMenu(){
        clear("База данных");
        body.addView(card("Текущая база", currentUri==null?"Не выбрана":"Подключена", currentUri==null?"Выберите hryushki_accounts_data.json":"Изменения можно сохранить в этот же файл"));
        body.addView(primary("Загрузить JSON",v->openDatabase()));
        body.addView(primary("Сохранить в текущий файл",v->{if(currentUri==null)toast("Сначала загрузите базу");else saveToUri(currentUri);}));
        body.addView(primary("Экспортировать копию JSON",v->exportDatabase()));
    }

    private void openDatabase(){
        Intent i=new Intent(Intent.ACTION_OPEN_DOCUMENT);i.addCategory(Intent.CATEGORY_OPENABLE);i.setType("application/json");i.putExtra(Intent.EXTRA_MIME_TYPES,new String[]{"application/json","text/plain","application/octet-stream"});startActivityForResult(i,OPEN_DB);
    }
    private void exportDatabase(){Intent i=new Intent(Intent.ACTION_CREATE_DOCUMENT);i.addCategory(Intent.CATEGORY_OPENABLE);i.setType("application/json");i.putExtra(Intent.EXTRA_TITLE,"hryushki_accounts_data.json");startActivityForResult(i,EXPORT_DB);}

    @Override protected void onActivityResult(int requestCode,int resultCode,Intent data){super.onActivityResult(requestCode,resultCode,data);if(resultCode!=RESULT_OK||data==null||data.getData()==null)return;Uri uri=data.getData();if(requestCode==OPEN_DB)loadFromUri(uri);else if(requestCode==EXPORT_DB)saveToUri(uri);}

    private void loadFromUri(Uri uri){
        try{
            getContentResolver().takePersistableUriPermission(uri,Intent.FLAG_GRANT_READ_URI_PERMISSION|Intent.FLAG_GRANT_WRITE_URI_PERMISSION);
        }catch(Exception ignored){}
        try(InputStream in=getContentResolver().openInputStream(uri)){
            byte[] buf=readAll(in); JSONObject parsed=new JSONObject(new String(buf,StandardCharsets.UTF_8));
            if(parsed.optJSONArray("operations")==null) throw new JSONException("нет массива operations");
            db=parsed; currentUri=uri; status.setText("База загружена • "+ops().length()+" операций"); toast("База загружена"); showOverview();
        }catch(Exception e){toast("Ошибка базы: "+e.getMessage());}
    }

    private void saveToUri(Uri uri){
        try(OutputStream out=getContentResolver().openOutputStream(uri,"wt")){
            if(out==null)throw new IOException("Нет доступа к файлу");out.write(db.toString(2).getBytes(StandardCharsets.UTF_8));out.flush();status.setText("Сохранено • "+new SimpleDateFormat("HH:mm",Locale.getDefault()).format(new Date()));toast("База сохранена");
        }catch(Exception e){toast("Не удалось сохранить: "+e.getMessage());}
    }
    private byte[] readAll(InputStream in)throws IOException{ByteArrayOutputStream b=new ByteArrayOutputStream();byte[] x=new byte[8192];int n;while((n=in.read(x))!=-1)b.write(x,0,n);return b.toByteArray();}
    private String money(double v){return String.format(Locale.forLanguageTag("ru-RU"),"%,.0f ₽",v).replace('\u00A0',' ');}
    private int dp(int v){return (int)(v*getResources().getDisplayMetrics().density+0.5f);}
    private void toast(String s){Toast.makeText(this,s,Toast.LENGTH_SHORT).show();}
}
