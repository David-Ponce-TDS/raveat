import { SpeechRecognition } from '@capacitor-community/speech-recognition';
import { Capacitor } from '@capacitor/core';

// Dictado por voz. En el telefono, el reconocedor nativo; en el navegador, Web Speech API, para
// probar sin instalar el APK. Mismo contrato que la camara: {ok, ..., mensaje}, nunca excepciones.

const IDIOMA = 'es-AR';

let reconocedor_activo = null;

function reconocedor_web(){
	return window.SpeechRecognition || window.webkitSpeechRecognition || null;
}

// Que el plugin este instalado no dice que el telefono sepa dictar: el reconocimiento lo presta
// un servicio del sistema (la app de Google) que puede faltar. `available()` es el que sabe.
export async function dictado_disponible(){
	if(!Capacitor.isNativePlatform()) return Boolean(reconocedor_web());
	try{
		const resultado = await SpeechRecognition.available();
		return Boolean(resultado && resultado.available);
	}catch{
		return false;
	}
}

// Primero el servicio, despues el permiso: pedir el microfono para un reconocedor que no existe
// es molestar con un permiso que no sirve.
async function permiso_nativo(){
	const estado = await SpeechRecognition.checkPermissions();
	if(estado.speechRecognition == 'granted') return true;
	const pedido = await SpeechRecognition.requestPermissions();
	return pedido.speechRecognition == 'granted';
}

// Devuelve {ok, texto, mensaje} cuando termina de escuchar. Sin voz no es un error: texto vacio.
export async function dictar(){
	if(Capacitor.isNativePlatform()) return dictar_nativo();
	if(reconocedor_web()) return dictar_web();
	return {ok: false, texto: '', mensaje: 'El dictado por voz no está disponible en este dispositivo.'};
}

// `partialResults: false` no es cosmetico: con `true`, `start()` resuelve en el acto y vacio, y
// el texto llega por un evento aparte. Con `false`, resuelve al terminar y trae lo dictado.
async function dictar_nativo(){
	if(!await dictado_disponible()){
		return {ok: false, texto: '', mensaje: 'El teléfono no tiene reconocimiento de voz.'};
	}
	if(!await permiso_nativo()){
		return {ok: false, texto: '', mensaje: 'Sin permiso de micrófono. Habilitalo en los ajustes del teléfono.'};
	}
	try{
		const resultado = await SpeechRecognition.start({language: IDIOMA, partialResults: false, popup: false, maxResults: 1});
		const texto = resultado && resultado.matches && resultado.matches.length > 0 ? resultado.matches[0] : '';
		return {ok: true, texto, mensaje: null};
	}catch(error){
		const original = String((error && error.message) || '');
		// Silencio o corte manual: no hay nada que avisar.
		if(/no match|no speech|client side|stop/i.test(original)) return {ok: true, texto: '', mensaje: null};
		if(/network|server/i.test(original)) return {ok: false, texto: '', mensaje: 'El reconocimiento de voz necesita conexión.'};
		return {ok: false, texto: '', mensaje: 'No se pudo escuchar el dictado.'};
	}
}

// El nativo se corta solo con el silencio; este boton lo corta antes.
export function detener_dictado(){
	if(Capacitor.isNativePlatform()) SpeechRecognition.stop();
	else if(reconocedor_activo) reconocedor_activo.stop();
}

// El reconocimiento web no responde a un stop() global: hay que guardar el que esta escuchando.
function dictar_web(){
	return new Promise(resolver => {
		const Reconocedor = reconocedor_web();
		const reconocedor = new Reconocedor();
		let texto = '';
		reconocedor.lang = IDIOMA;
		reconocedor.maxAlternatives = 1;
		reconocedor.onresult = evento => {
			texto = evento.results[0][0].transcript;
		};
		reconocedor.onerror = evento => {
			if(evento.error == 'not-allowed') resolver({ok: false, texto: '', mensaje: 'Sin permiso de micrófono. Habilitalo en el navegador.'});
		};
		reconocedor.onend = () => {
			reconocedor_activo = null;
			resolver({ok: true, texto, mensaje: null});
		};
		reconocedor_activo = reconocedor;
		reconocedor.start();
	});
}
