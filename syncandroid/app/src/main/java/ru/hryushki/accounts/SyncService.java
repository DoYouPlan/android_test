package ru.hryushki.accounts;
import android.content.*;
import android.util.AtomicFile;
import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import javax.crypto.*;
import javax.crypto.spec.GCMParameterSpec;
import android.security.keystore.*;
public final class SyncService {
 public static final String URL="https://jkybdiulsztoilkwmhij.supabase.co",KEY="sb_publishable_iVHcUXMDWE1D5XqOJ9sRXw_jhArHGUA";
 private Context context;private JSONObject tokens;public String email="",owner="";private long expires;
 public SyncService(Context c)throws Exception{context=c;File f=new File(c.getFilesDir(),"sync-session.enc");if(f.exists())try{JSONObject t=new JSONObject(new String(crypt(new AtomicFile(f).readFully(),false),StandardCharsets.UTF_8));tokens=t;owner=t.getString("owner");email=t.getString("email");}catch(Exception e){throw new IOException("Не удалось прочитать вход. Выйдите и войдите снова");}}
 public boolean signed(){return tokens!=null;}
 private byte[] crypt(byte[] data,boolean encrypt)throws Exception{String alias="hryushki-sync-token";KeyStore ks=KeyStore.getInstance("AndroidKeyStore");ks.load(null);if(!ks.containsAlias(alias)){KeyGenerator g=KeyGenerator.getInstance("AES","AndroidKeyStore");g.init(new KeyGenParameterSpec.Builder(alias,KeyProperties.PURPOSE_ENCRYPT|KeyProperties.PURPOSE_DECRYPT).setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).build());g.generateKey();}javax.crypto.SecretKey key=(javax.crypto.SecretKey)ks.getKey(alias,null);Cipher c=Cipher.getInstance("AES/GCM/NoPadding");if(encrypt){c.init(Cipher.ENCRYPT_MODE,key);byte[] encrypted=c.doFinal(data),iv=c.getIV(),out=new byte[1+iv.length+encrypted.length];out[0]=(byte)iv.length;System.arraycopy(iv,0,out,1,iv.length);System.arraycopy(encrypted,0,out,1+iv.length,encrypted.length);return out;}int len=data[0]&255;c.init(Cipher.DECRYPT_MODE,key,new GCMParameterSpec(128,data,1,len));return c.doFinal(data,1+len,data.length-1-len);}
 private void remember(JSONObject t)throws Exception{owner=t.getJSONObject("user").getString("id");email=t.getJSONObject("user").optString("email");tokens=new JSONObject();tokens.put("refresh_token",t.getString("refresh_token"));tokens.put("access_token",t.getString("access_token"));tokens.put("owner",owner);tokens.put("email",email);expires=System.currentTimeMillis()+t.optLong("expires_in",3600)*1000-60000;AtomicFile f=new AtomicFile(new File(context.getFilesDir(),"sync-session.enc"));FileOutputStream out=null;try{out=f.startWrite();out.write(crypt(tokens.toString().getBytes(StandardCharsets.UTF_8),true));f.finishWrite(out);}catch(Exception e){if(out!=null)f.failWrite(out);throw e;}}
 public synchronized void login(String email,String password)throws Exception{JSONObject p=new JSONObject();p.put("email",email);p.put("password",password);remember(call("/auth/v1/token?grant_type=password",p,null));}
 public synchronized void logout(){tokens=null;expires=0;owner="";email="";new AtomicFile(new File(context.getFilesDir(),"sync-session.enc")).delete();}
 public synchronized JSONObject exchange(JSONArray changes)throws Exception{if(!signed())throw new IOException("Войдите в тестовый аккаунт");if(expires<System.currentTimeMillis()){JSONObject p=new JSONObject();p.put("refresh_token",tokens.getString("refresh_token"));remember(call("/auth/v1/token?grant_type=refresh_token",p,null));}JSONObject p=new JSONObject();p.put("p_changes",changes);return call("/rest/v1/rpc/hry_sync_exchange",p,tokens.getString("access_token"));}
 private JSONObject call(String route,JSONObject payload,String token)throws Exception{HttpURLConnection c=(HttpURLConnection)new java.net.URL(URL+route).openConnection();c.setConnectTimeout(10000);c.setReadTimeout(15000);c.setRequestMethod("POST");c.setRequestProperty("apikey",KEY);c.setRequestProperty("Content-Type","application/json");if(token!=null)c.setRequestProperty("Authorization","Bearer "+token);c.setDoOutput(true);try{try(OutputStream out=c.getOutputStream()){out.write(payload.toString().getBytes(StandardCharsets.UTF_8));}int status=c.getResponseCode();InputStream in=status>=400?c.getErrorStream():c.getInputStream();String body;try(InputStream stream=in){body=new String(DatabaseStore.read(stream),StandardCharsets.UTF_8);}JSONObject r=new JSONObject(body);if(status>=400)throw new IOException("HTTP "+status+": "+r.optString("msg",r.optString("message",r.optString("error_description","Ошибка сервиса"))));return r;}finally{c.disconnect();}}
}
