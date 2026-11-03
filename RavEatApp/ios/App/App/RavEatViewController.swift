import Capacitor

// El controlador de Capacitor, con los plugins propios registrados. Un plugin que no viene de npm
// no lo descubre nadie: hay que darlo de alta aca, cuando el bridge ya existe. Es el equivalente
// del registerPlugin(GoogleAuthPlugin.class) de MainActivity en Android.
class RavEatViewController: CAPBridgeViewController {
    override func capacitorDidLoad() {
        bridge?.registerPluginInstance(GoogleAuthPlugin())
    }
}
