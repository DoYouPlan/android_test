package ru.hryushki.accounts;

import android.content.*;
import android.net.Uri;
import android.util.AtomicFile;
import org.json.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;

/** The private atomic database is authoritative. Documents are explicit imports/exports. */
public final class DatabaseStore {
 private final Context context;private final AtomicFile file;private final File backups;private final android.content.SharedPreferences prefs;
 public Runnable onSaved;public JSONObject data;public String loadError="";
 public DatabaseStore(Context context)throws Exception{this.context=context;file=new AtomicFile(new File(context.getFilesDir(),"accounts.json"));backups=new File(context.getFilesDir(),"backups");prefs=context.getSharedPreferences("database",0);if(file.getBaseFile().exists()||new File(file.getBaseFile()+".bak").exists()){try{data=parse(file.readFully());}catch(Exception e){loadError="Не удалось открыть сохранённую базу: "+e.getMessage();data=null;}}else{data=Engine.empty();write(data);}}
 static JSONObject parse(byte[] bytes)throws Exception{JSONObject d=new JSONObject(new String(bytes,StandardCharsets.UTF_8).replaceFirst("^\\ufeff",""));Engine.validate(d);return d;}
 public static byte[] read(InputStream in)throws IOException{if(in==null)throw new IOException("Файл недоступен");ByteArrayOutputStream b=new ByteArrayOutputStream();byte[] buf=new byte[8192];int n;while((n=in.read(buf))!=-1){if(b.size()+n>32*1024*1024)throw new IOException("База больше 32 МБ");b.write(buf,0,n);}return b.toByteArray();}
 static byte[] bytes(JSONObject d){return d.toString().getBytes(StandardCharsets.UTF_8);}
 static String hash(byte[] bytes)throws Exception{byte[] h=MessageDigest.getInstance("SHA-256").digest(bytes);StringBuilder b=new StringBuilder();for(byte x:h)b.append(String.format(Locale.ROOT,"%02x",x));return b.toString();}
 private void write(JSONObject d)throws Exception{byte[] b=bytes(d);FileOutputStream out=null;try{out=file.startWrite();out.write(b);file.finishWrite(out);}catch(Exception e){if(out!=null)file.failWrite(out);throw e;}}
 public synchronized void save(JSONObject next)throws Exception{if(next.has("_sync"))SyncModel.ids(next,false);Engine.validate(next);if(data!=null)backup();write(next);data=next;loadError="";if(onSaved!=null)onSaved.run();}
 public synchronized File backup()throws Exception{if(data==null)throw new IOException("Нет открытой базы");if(!backups.exists()&&!backups.mkdirs())throw new IOException("Не удалось создать папку копий");File f=File.createTempFile("accounts_"+System.currentTimeMillis()+"_",".json",backups);try(FileOutputStream out=new FileOutputStream(f)){out.write(bytes(data));out.getFD().sync();}File[] files=listBackups();for(int i=30;i<files.length;i++)if(!files[i].delete())break;return f;}
 public File[] listBackups(){File[] files=backups.listFiles((dir,name)->name.endsWith(".json"));if(files==null)return new File[0];Arrays.sort(files,(a,b)->Long.compare(b.lastModified(),a.lastModified()));return files;}
 public void restore(File f)throws Exception{JSONObject next;try(FileInputStream in=new FileInputStream(f)){next=parse(read(in));}save(next);}
 public JSONObject readDocument(Uri uri)throws Exception{try(InputStream in=context.getContentResolver().openInputStream(uri)){return parse(read(in));}}
 public void importDocument(Uri uri,int flags)throws Exception{byte[] b;try(InputStream in=context.getContentResolver().openInputStream(uri)){b=read(in);}JSONObject next=parse(b);save(next);try{context.getContentResolver().takePersistableUriPermission(uri,flags&(Intent.FLAG_GRANT_READ_URI_PERMISSION|Intent.FLAG_GRANT_WRITE_URI_PERMISSION));}catch(SecurityException ignored){}prefs.edit().putString("uri",uri.toString()).putString("hash",hash(b)).commit();}
 public String document(){return prefs.getString("uri","");}
 public void export(Uri uri,byte[] content)throws Exception{try(OutputStream out=context.getContentResolver().openOutputStream(uri,"wt")){if(out==null)throw new IOException("Нет доступа к файлу");out.write(content);out.flush();}}
 public void exportCurrent()throws Exception{String last=document();if(last.isEmpty())throw new IOException("Сначала импортируйте файл или экспортируйте копию");Uri uri=Uri.parse(last);byte[] existing;try(InputStream in=context.getContentResolver().openInputStream(uri)){existing=read(in);}if(!hash(existing).equals(prefs.getString("hash","")))throw new IOException("Внешний файл изменён. Экспортируйте копию под новым именем или импортируйте обновлённый файл. Перезапись остановлена");byte[] b=bytes(data);export(uri,b);prefs.edit().putString("hash",hash(b)).commit();}
 public byte[] rawSaved()throws Exception{return file.readFully();}
}
