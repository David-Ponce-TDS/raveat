import { Capacitor } from '@capacitor/core';
import { Geolocation } from '@capacitor/geolocation';

// Mismo patron que la camara: {ok, ..., mensaje} y un permiso con final abierto. A diferencia de
// la camara, aca el camino web es completo: el plugin cae a la geolocalizacion del navegador.
export async function obtener_ubicacion_actual(){
	if(!Capacitor.isPluginAvailable('Geolocation')){
		return {ok: false, ubicacion: null, mensaje: 'La geolocalización no está disponible en este dispositivo.'};
	}
	try{
		const permisos = await Geolocation.requestPermissions();
		// 'coarse' (aproximada) alcanza: para una direccion de entrega no hacen falta decimales
		// de GPS militar, y Android 12+ deja al usuario elegir darla aproximada.
		if(permisos.location != 'granted' && permisos.coarseLocation != 'granted'){
			return {ok: false, ubicacion: null, mensaje: 'Sin permiso de ubicación. Habilitalo en los ajustes del teléfono.'};
		}
		const posicion = await Geolocation.getCurrentPosition({enableHighAccuracy: true, timeout: 12000, maximumAge: 60000});
		return {
			ok: true,
			ubicacion: {
				latitud: posicion.coords.latitude,
				longitud: posicion.coords.longitude,
				precision: posicion.coords.accuracy
			},
			mensaje: null
		};
	}catch(error){
		return {ok: false, ubicacion: null, mensaje: (error && error.message) || 'No se pudo obtener la ubicación.'};
	}
}

// El respaldo del mapa: la direccion escrita, convertida en puntos por Nominatim, el buscador
// gratuito de OpenStreetMap. Sin clave de API; a cambio, se consulta al enviar y no por tecla.
export async function buscar_direcciones(texto){
	const consulta = String(texto || '').trim();
	if(consulta.length < 3) return {ok: true, resultados: [], mensaje: null};
	try{
		const parametros = new URLSearchParams({format: 'json', q: consulta, limit: '5', addressdetails: '1', 'accept-language': 'es'});
		const respuesta = await fetch(`https://nominatim.openstreetmap.org/search?${parametros.toString()}`, {
			headers: {Accept: 'application/json'}
		});
		if(!respuesta.ok) return {ok: false, resultados: [], mensaje: 'No se pudo buscar la dirección.'};
		const datos = await respuesta.json();
		const resultados = datos.map(item => ({
			nombre: item.display_name,
			direccion: direccion_corta(item),
			latitud: Number(item.lat),
			longitud: Number(item.lon)
		}));
		return {ok: true, resultados, mensaje: null};
	}catch{
		return {ok: false, resultados: [], mensaje: 'No se pudo buscar la dirección. Revisá la conexión.'};
	}
}

// El camino inverso: la direccion del punto donde quedo el pin. Sin red no hay direccion, y el
// formulario se queda con la que estaba escrita.
export async function buscar_direccion_de_punto(latitud, longitud){
	try{
		const parametros = new URLSearchParams({format: 'json', lat: String(latitud), lon: String(longitud), zoom: '18', addressdetails: '1', 'accept-language': 'es'});
		const respuesta = await fetch(`https://nominatim.openstreetmap.org/reverse?${parametros.toString()}`, {
			headers: {Accept: 'application/json'}
		});
		if(!respuesta.ok) return {ok: false, direccion: '', mensaje: 'No se pudo leer la dirección del punto.'};
		const direccion = direccion_corta(await respuesta.json());
		if(!direccion) return {ok: false, direccion: '', mensaje: 'Ese punto no tiene una dirección conocida.'};
		return {ok: true, direccion, mensaje: null};
	}catch{
		return {ok: false, direccion: '', mensaje: 'No se pudo leer la dirección del punto. Revisá la conexión.'};
	}
}

// "Junin 801, San Luis" y no los nueve tramos de display_name: lo que se escribiria a mano.
function direccion_corta(item){
	const partes = item.address || {};
	const calle = partes.road || partes.pedestrian || partes.footway || partes.path || '';
	const numero = partes.house_number || '';
	const localidad = partes.town || partes.village || partes.city_district || partes.city || '';
	if(!calle) return String(item.display_name || '').split(', ').slice(0, 3).join(', ');
	return [`${calle} ${numero}`.trim(), localidad].filter(Boolean).join(', ');
}

export function formatear_coordenadas(latitud, longitud){
	if(latitud == null || longitud == null) return '';
	return `${Number(latitud).toFixed(6)}, ${Number(longitud).toFixed(6)}`;
}

// Para VER el punto, el mapa es externo: la app de mapas del telefono ya lo resuelve. El mapa
// propio solo sirve para MARCARLO (comp_selector_ubicacion); con marcadores es el extra X07.
export function url_mapa(latitud, longitud, direccion = ''){
	if(latitud != null && longitud != null){
		return `https://www.google.com/maps/search/?api=1&query=${latitud},${longitud}`;
	}
	return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(direccion)}`;
}

// "Ruta" en vez de "mapa": abre el navegador ya calculando como llegar. Para el repartidor en la
// calle, ver el punto y despues pedir indicaciones son dos pasos; esto es uno.
export function url_ruta(latitud, longitud, direccion = ''){
	const destino = latitud != null && longitud != null ? `${latitud},${longitud}` : direccion;
	return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destino)}`;
}
