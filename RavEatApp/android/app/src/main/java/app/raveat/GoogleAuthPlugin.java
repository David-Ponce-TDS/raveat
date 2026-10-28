package app.raveat;

import android.app.Activity;

import androidx.activity.result.ActivityResult;

import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.ActivityCallback;
import com.getcapacitor.annotation.CapacitorPlugin;
import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.google.android.gms.auth.api.signin.GoogleSignInStatusCodes;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.tasks.Task;

// Plugin propio de Capacitor: no todo lo nativo viene en un paquete de npm. El ingreso con Google
// necesita la pantalla de cuentas del sistema, que es una Activity, y eso solo se resuelve desde
// Java. Del lado del JavaScript esto se ve como cualquier otro plugin (registerPlugin('GoogleAuth')).
//
// Lo unico que devuelve y de verdad importa es el `id_token`: la API no confia en el email que
// mande el telefono, confia en ese token firmado por Google.
@CapacitorPlugin(name = "GoogleAuth")
public class GoogleAuthPlugin extends Plugin {
    private GoogleSignInClient client;

    @PluginMethod
    public void signIn(PluginCall call) {
        String webClientId = call.getString("web_client_id", "");
        if(webClientId == null || webClientId.trim().isEmpty()) {
            call.reject("Falta el web client id de Google.", "SIN_CLIENT_ID");
            return;
        }

        GoogleSignInOptions options = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
            .requestEmail()
            .requestProfile()
            // requestIdToken pide el token de identidad, que es lo unico que le sirve a la API.
            // Sin esta linea el login "funciona" en el telefono y despues no hay nada que validar.
            .requestIdToken(webClientId)
            .build();

        client = GoogleSignIn.getClient(getActivity(), options);
        // Google RECUERDA la ultima cuenta usada, y esa memoria vive en el dispositivo, no en la
        // app. Sin este `signOut` previo, el segundo intento reusa la misma cuenta sin mostrar el
        // selector: si justo esa es la que la API rechaza —un email que no esta dado de alta—, el
        // mismo error se repite para siempre y no hay forma de elegir otra.
        // Se paga un toque de mas en el caso feliz a cambio de que "entrar con otra cuenta" sea
        // posible, que en un local con varios empleados no es un caso raro.
        client.signOut().addOnCompleteListener(tarea ->
            startActivityForResult(call, client.getSignInIntent(), "manejarResultadoLogin"));
    }

    @PluginMethod
    public void signOut(PluginCall call) {
        if(client == null) {
            GoogleSignInOptions options = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN).build();
            client = GoogleSignIn.getClient(getActivity(), options);
        }

        client.signOut()
            .addOnSuccessListener(unused -> call.resolve())
            .addOnFailureListener(error -> call.reject("No se pudo cerrar la sesión de Google.", error));
    }

    @ActivityCallback
    private void manejarResultadoLogin(PluginCall call, ActivityResult result) {
        if(call == null) return;
        // Volver con el boton "atras" no trae datos, y `getSignedInAccountFromIntent(null)` falla
        // con un codigo generico: se mostraba el mensajon de SIGN_IN_FAILED por haber cancelado.
        // Cancelar no es un error, y decir lo contrario manda a revisar la consola de Google por
        // nada.
        if(result.getResultCode() == Activity.RESULT_CANCELED && result.getData() == null) {
            call.reject("Cancelaste el ingreso con Google.", String.valueOf(GoogleSignInStatusCodes.SIGN_IN_CANCELLED));
            return;
        }
        Task<GoogleSignInAccount> task = GoogleSignIn.getSignedInAccountFromIntent(result.getData());
        try {
            GoogleSignInAccount cuenta = task.getResult(ApiException.class);
            String idToken = cuenta.getIdToken();
            if(idToken == null || idToken.isEmpty()) {
                call.reject("Google no devolvió el token de identidad. Revisá que el web client id sea el cliente OAuth de tipo Aplicación web.", "SIN_ID_TOKEN");
                return;
            }
            JSObject respuesta = new JSObject();
            respuesta.put("email", cuenta.getEmail());
            respuesta.put("nombre", cuenta.getDisplayName());
            respuesta.put("id_token", idToken);
            call.resolve(respuesta);
        }
        catch(ApiException error) {
            call.reject(explicar(error.getStatusCode()), String.valueOf(error.getStatusCode()), error);
        }
    }

    // Los codigos de Google Sign-In son numeros sin contexto, y los dos que aparecen siempre en
    // clase tienen causas concretas. El mensaje las enumera en orden de frecuencia para no perder
    // una tarde mirando la consola de Google Cloud sin saber que buscar.
    private String explicar(int codigo) {
        if(codigo == GoogleSignInStatusCodes.SIGN_IN_CANCELLED) {
            return "Cancelaste el ingreso con Google.";
        }
        // DEVELOPER_ERROR (10): la configuracion de Google Cloud no coincide con esta APK.
        if(codigo == GoogleSignInStatusCodes.DEVELOPER_ERROR) {
            return "Google Sign-In: DEVELOPER_ERROR (10). Revisá, en este orden: "
                + "1) que VITE_GOOGLE_WEB_CLIENT_ID sea el cliente OAuth de tipo Aplicación web "
                + "y NO el de tipo Android (requestIdToken solo acepta el web); "
                + "2) que exista un cliente OAuth de tipo Android con el paquete app.raveat y la "
                + "SHA-1 del keystore que firma esta APK (debug y release firman distinto); "
                + "3) que los dos clientes sean del mismo proyecto de Google.";
        }
        // SIGN_IN_FAILED (12500): la APK se identifico bien; lo que falta esta en la consola.
        if(codigo == GoogleSignInStatusCodes.SIGN_IN_FAILED) {
            return "Google Sign-In: SIGN_IN_FAILED (12500). La APK se identificó bien, así que "
                + "falta configuración en Google Cloud: la pantalla de consentimiento de OAuth, y "
                + "si está en modo Testing, tu cuenta tiene que figurar en Usuarios de prueba.";
        }
        return "No se pudo entrar con Google (código " + codigo + ": " + GoogleSignInStatusCodes.getStatusCodeString(codigo) + ").";
    }
}
