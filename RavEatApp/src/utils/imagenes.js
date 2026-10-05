import { obtener_api_url } from '@/config/debug';

// La API guarda rutas relativas (`/seed/productos/pizza.jpg`), no URLs completas: si guardara el
// host, mover la API de maquina invalidaria todas las imagenes de la base.
export function resolver_url_imagen(url, imagen_default = ''){
	if(!url) return imagen_default;
	if(url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:') || url.startsWith('blob:')) return url;
	if(url.startsWith('/')) return `${obtener_api_url()}${url}`;
	return url;
}

// Carga el archivo como imagen antes de abrir el recorte: si no es una imagen legible, mejor
// avisarlo aca que dejar al recorte con un cuadro en blanco.
export function cargar_imagen_archivo(archivo){
	return new Promise((resolve, reject) =>{
		const url = URL.createObjectURL(archivo);
		const imagen = new Image();
		imagen.onload = () =>{
			URL.revokeObjectURL(url);
			resolve(imagen);
		};
		imagen.onerror = () =>{
			URL.revokeObjectURL(url);
			reject(new Error('No se pudo leer la imagen elegida.'));
		};
		imagen.src = url;
	});
}

// Convierte el canvas del recorte en un File JPG listo para subir por FormData. JPG al 90 % y
// 768 px de lado: la foto de un telefono pesa varios MB y la carta la muestra en 150 px.
export function canvas_a_archivo(canvas, nombre = 'producto'){
	return new Promise((resolve, reject) =>{
		canvas.toBlob(blob =>{
			if(!blob){
				reject(new Error('No se pudo recortar la imagen.'));
				return;
			}
			resolve(new File([blob], `${nombre}.jpg`, {type: 'image/jpeg', lastModified: Date.now()}));
		}, 'image/jpeg', 0.9);
	});
}
