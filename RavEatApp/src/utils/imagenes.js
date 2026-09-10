import { obtener_api_url } from '@/config/debug';

// La API guarda rutas relativas (`/seed/productos/pizza.jpg`), no URLs completas: si guardara el
// host, mover la API de maquina invalidaria todas las imagenes de la base.
export function resolver_url_imagen(url, imagen_default = ''){
	if(!url) return imagen_default;
	if(url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:') || url.startsWith('blob:')) return url;
	if(url.startsWith('/')) return `${obtener_api_url()}${url}`;
	return url;
}
