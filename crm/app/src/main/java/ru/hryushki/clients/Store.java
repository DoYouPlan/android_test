package ru.hryushki.clients;
import android.content.Context;
import android.util.AtomicFile;
import org.json.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
public final class Store {
 public static final Object LOCK=new Object();public final Context context;public JSONObject data;private final AtomicFile file;private final File backups;
 public interface Change{void apply(JSONObject d)throws Exception;}
 public Store(Context ctx)throws Exception{context=ctx.getApplicationContext();file=new AtomicFile(new File(context.getFilesDir(),"clients-orders.json"));backups=new File(context.getFilesDir(),"crm-backups");synchronized(LOCK){if(!file.getBaseFile().exists()&&!new File(file.getBaseFile()+".bak").exists())write(Model.empty());data=parse(file.readFully());}}
 public static byte[] read(InputStream in)throws IOException{if(in==null)throw new IOException("Файл недоступен");ByteArrayOutputStream b=new ByteArrayOutputStream();byte[] buf=new byte[8192];int n;while((n=in.read(buf))!=-1){if(b.size()+n>20*1024*1024)throw new IOException("Файл больше 20 МБ");b.write(buf,0,n);}return b.toByteArray();}
 public static JSONObject parse(byte[] bytes)throws Exception{JSONObject d=new JSONObject(new String(bytes,StandardCharsets.UTF_8).replaceFirst("^\\ufeff",""));Model.validate(d);return d;}
 public byte[] bytes(){return data.toString().getBytes(StandardCharsets.UTF_8);}
 void write(JSONObject d)throws Exception{FileOutputStream out=null;try{out=file.startWrite();out.write(d.toString().getBytes(StandardCharsets.UTF_8));file.finishWrite(out);}catch(Exception e){if(out!=null)file.failWrite(out);throw e;}}
 public void reload()throws Exception{synchronized(LOCK){data=parse(file.readFully());}}
 public void change(Change c)throws Exception{synchronized(LOCK){JSONObject next=parse(file.readFully());c.apply(next);Model.validate(next);backup();JSONArray audit=Model.rows(next,"audit");while(audit.length()>500)audit.remove(0);write(next);data=next;}}
 public void replace(JSONObject next)throws Exception{synchronized(LOCK){Model.validate(next);backup();write(next);data=next;}}
 public File backup()throws Exception{synchronized(LOCK){if(!backups.exists()&&!backups.mkdirs())throw new IOException("Не удалось создать копию");File f=File.createTempFile("crm_"+System.currentTimeMillis()+"_",".json",backups);try(FileOutputStream out=new FileOutputStream(f)){out.write(file.readFully());out.getFD().sync();}File[] all=listBackups();for(int i=20;i<all.length;i++)all[i].delete();return f;}}
 public File[] listBackups(){File[] a=backups.listFiles((d,n)->n.endsWith(".json"));if(a==null)return new File[0];Arrays.sort(a,(x,y)->Long.compare(y.lastModified(),x.lastModified()));return a;}
 public byte[] raw()throws Exception{return file.readFully();}
}
