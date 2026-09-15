import { ajax_request } from './ajax_service';
import { construir_query } from '@/utils/consulta';

// La vitrina: la carta entera con sus categorias y los contadores. No pagina porque una carta de
// restaurante entra en una respuesta.
export function obtener_resumen_productos(){
	return ajax_request({endpoint: '/api/productos/resumen', metodo: 'GET'});
}

// El catalogo de gestion: paginado, y con la busqueda y los filtros resueltos en la API.
export function obtener_listado_productos(opciones = {}){
	const query = construir_query({
		busqueda: opciones.busqueda,
		categoria_id: opciones.categoria_id,
		disponible: opciones.disponible,
		pagina: opciones.pagina,
		tamano: opciones.tamano
	});
	return ajax_request({endpoint: `/api/productos/listado${query}`, metodo: 'GET'});
}

export function obtener_producto(id){
	return ajax_request({endpoint: `/api/productos/${id}`, metodo: 'GET'});
}

// Alta y edicion viajan como multipart/form-data: un JSON no puede llevar un archivo adentro.
// La foto es un campo mas ('imagen'); la ruta donde se guarda la decide el servidor.
export function crear_producto(datos, imagen = null){
	return ajax_request({endpoint: '/api/productos', metodo: 'POST', datos: armar_form_data(datos, imagen)});
}

export function actualizar_producto(id, datos, imagen = null){
	return ajax_request({endpoint: `/api/productos/${id}`, metodo: 'PUT', datos: armar_form_data(datos, imagen)});
}

// En un FormData todo viaja como texto: un null llegaria como "null", por eso no se manda.
function armar_form_data(datos, imagen){
	const formulario = new FormData();
	Object.keys(datos).forEach(campo =>{
		const valor = datos[campo];
		if(valor != null && valor != undefined) formulario.append(campo, valor);
	});
	if(imagen) formulario.append('imagen', imagen);
	return formulario;
}

export function eliminar_producto(id){
	return ajax_request({endpoint: `/api/productos/${id}`, metodo: 'DELETE'});
}
