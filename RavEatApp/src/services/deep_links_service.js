import { App } from '@capacitor/app';

// Abrir la app desde afuera y aterrizar donde corresponde. El enlace que llega es el del QR del
// pedido, `https://<dominio>/p/<token>`: con los App Links verificados Android abre la app en vez
// del navegador, y aca se traduce a la ruta de la app con el mismo camino, `/p/<token>`.
// El guard corre igual: un deep link no saltea el login ni los roles, solo propone un destino.

export function iniciar_deep_links(router){
	App.addListener('appUrlOpen', function(evento){
		const ruta = ruta_de_enlace(evento.url);
		if(ruta) router.push(ruta);
	});
	// `appUrlOpen` avisa de los enlaces que llegan con la app YA abierta. Si el enlace fue lo que
	// la abrio, hay que ir a buscarlo: sin esto, escanear el QR con la app cerrada la abre en la
	// vitrina y el pedido se pierde en el camino. En el navegador no hay enlace de arranque.
	App.getLaunchUrl()
		.then(function(resultado){
			const ruta = ruta_de_enlace((resultado && resultado.url) || '');
			if(ruta) router.push(ruta);
		})
		.catch(function(){});
}

// El token que viaja en un QR nuestro, o null si el texto escaneado es otra cosa (un ticket viejo
// con el codigo, por ejemplo). Se apoya en la misma lectura que el deep link.
export function token_de_enlace(texto){
	const ruta = ruta_de_enlace(texto);
	return ruta ? ruta.slice(3) : null;
}

function ruta_de_enlace(url){
	try{
		const segmentos = new URL(url).pathname.split('/').filter(Boolean);
		if(segmentos.length == 2 && segmentos[0] == 'p') return `/p/${segmentos[1]}`;
		return null;
	}catch{
		// Una URL que no se puede leer no es un error: es un enlace que no era para nosotros.
		return null;
	}
}
