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

export function formatear_coordenadas(latitud, longitud){
	if(latitud == null || longitud == null) return '';
	return `${Number(latitud).toFixed(6)}, ${Number(longitud).toFixed(6)}`;
}

// El mapa es EXTERNO a proposito: abrir la app de mapas del telefono con las coordenadas ya
// resuelve la clase. El mapa embebido con marcadores es el modulo extra X07.
export function url_mapa(latitud, longitud, direccion = ''){
	if(latitud != null && longitud != null){
		return `https://www.google.com/maps/search/?api=1&query=${latitud},${longitud}`;
	}
	return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(direccion)}`;
}
