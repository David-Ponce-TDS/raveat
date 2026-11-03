import { toastController } from '@ionic/vue';

// El aviso corto de la app: el cartelito que confirma que algo salio bien o que fallo. Es el
// tercer canal y el mas barato de los tres del modulo — no es la notificacion local (esa la
// dibuja el sistema operativo y sobrevive a cerrar la app) ni la bandeja (esa es una tabla en la
// API). Este vive dentro de la pantalla y dura dos segundos.
//
// Arriba y no abajo: abajo estan los tabs, y el aviso los tapaba justo cuando el usuario iba a
// tocar el siguiente paso.

export function avisar_exito(mensaje){
	return mostrar(mensaje, 'success', 2000);
}

export function avisar_error(mensaje){
	return mostrar(texto_para_el_usuario(mensaje), 'danger', 3500);
}

// Un codigo de plugin o una excepcion del sistema ("DEVELOPER_ERROR (10)", "TypeError: ...") no le
// dice nada a quien usa la app: se avisa solo "Error" y el detalle queda en la consola. Los
// mensajes que escribimos nosotros, en castellano, pasan tal cual.
const patron_tecnico = /_ERROR|[A-Z]{3,}_[A-Z_]{2,}|\(\d+\)\s*$|Exception|TypeError|ReferenceError|SyntaxError|undefined|\[object|not implemented|Failed to|is not a function|Network ?Error/;

function texto_para_el_usuario(mensaje){
	const texto = String(mensaje == null ? '' : mensaje).trim();
	if(texto && !patron_tecnico.test(texto)) return texto;
	if(texto) console.warn('[aviso] mensaje tecnico oculto:', texto);
	return 'Error';
}

async function mostrar(mensaje, color, duracion){
	const aviso = await toastController.create({
		message: mensaje,
		color,
		duration: duracion,
		position: 'top',
		buttons: [{text: 'OK', role: 'cancel'}]
	});
	await aviso.present();
	return aviso;
}
