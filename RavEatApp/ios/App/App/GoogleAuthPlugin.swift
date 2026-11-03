import Capacitor
import GoogleSignIn

// Plugin propio de Capacitor, el par en Swift del GoogleAuthPlugin.java de Android. Del lado del
// JavaScript es el mismo: registerPlugin('GoogleAuth') con signIn y signOut, y devuelve lo mismo.
//
// Lo unico que importa es el `id_token`: la API no confia en el email que mande el telefono,
// confia en ese token firmado por Google, y lo valida contra la audiencia. Por eso el token se pide
// con el web client id (serverClientID) y NO con el cliente de iOS: es el mismo que usa Android.
//
// Hace falta, en Info.plist: GIDClientID (el cliente OAuth de tipo iOS) y el esquema de URL
// invertido de ese cliente, que es por donde Google devuelve el control a la app.
@objc(GoogleAuthPlugin)
public class GoogleAuthPlugin: CAPPlugin, CAPBridgedPlugin {
    public let identifier = "GoogleAuthPlugin"
    public let jsName = "GoogleAuth"
    public let pluginMethods: [CAPPluginMethod] = [
        CAPPluginMethod(name: "signIn", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "signOut", returnType: CAPPluginReturnPromise)
    ]

    @objc func signIn(_ call: CAPPluginCall) {
        let webClientId = (call.getString("web_client_id") ?? "").trimmingCharacters(in: .whitespaces)
        if webClientId.isEmpty {
            call.reject("Falta el web client id de Google.", "SIN_CLIENT_ID")
            return
        }
        // Sin el ajuste GOOGLE_IOS_CLIENT, el Info.plist queda en ".apps.googleusercontent.com".
        guard let iosClientId = Bundle.main.object(forInfoDictionaryKey: "GIDClientID") as? String, !iosClientId.hasPrefix(".") else {
            call.reject("Falta el ajuste GOOGLE_IOS_CLIENT: es el cliente OAuth de tipo iOS.", "SIN_CLIENTE_IOS")
            return
        }

        DispatchQueue.main.async {
            guard let presentador = self.bridge?.viewController else {
                call.reject("No hay una pantalla desde la cual mostrar las cuentas de Google.", "SIN_PANTALLA")
                return
            }
            GIDSignIn.sharedInstance.configuration = GIDConfiguration(clientID: iosClientId, serverClientID: webClientId)
            // Google RECUERDA la ultima cuenta: sin este signOut previo, "entrar con otra cuenta" no
            // muestra el selector. Es lo mismo que hace el plugin de Android.
            GIDSignIn.sharedInstance.signOut()
            GIDSignIn.sharedInstance.signIn(withPresenting: presentador) { resultado, error in
                if let error = error as NSError? {
                    // Cancelar no es un error: se devuelve el mismo codigo que Android (12501) para
                    // que el JavaScript lo trate igual en las dos plataformas.
                    if error.domain == kGIDSignInErrorDomain && error.code == GIDSignInError.canceled.rawValue {
                        call.reject("Cancelaste el ingreso con Google.", "12501")
                    } else {
                        call.reject("No se pudo entrar con Google (\(error.domain) \(error.code)): \(error.localizedDescription)", String(error.code))
                    }
                    return
                }
                guard let usuario = resultado?.user, let idToken = usuario.idToken?.tokenString, !idToken.isEmpty else {
                    call.reject("Google no devolvió el token de identidad. Revisá que el web client id sea el cliente OAuth de tipo Aplicación web.", "SIN_ID_TOKEN")
                    return
                }
                call.resolve([
                    "email": usuario.profile?.email ?? "",
                    "nombre": usuario.profile?.name ?? "",
                    "id_token": idToken
                ])
            }
        }
    }

    @objc func signOut(_ call: CAPPluginCall) {
        GIDSignIn.sharedInstance.signOut()
        call.resolve()
    }
}
