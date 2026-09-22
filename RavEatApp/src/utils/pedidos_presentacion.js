// Como se PINTA un pedido: colores, etiquetas y cual es el siguiente estado a ofrecer. Vive en
// utils y no en la pantalla desde v7, cuando la lista dejo de ser la unica que lo dibuja: el
// modal de detalle muestra los mismos badges, y con dos copias se desincronizaban al primer
// estado nuevo. El store sigue guardando el estado; esto es solo su presentacion.

const COLOR_ESTADO = {
	borrador: 'medium',
	confirmado: 'primary',
	en_preparacion: 'warning',
	listo: 'success',
	entregado: 'success',
	cerrado: 'medium',
	cancelado: 'danger'
};

// El siguiente estado posible, para ofrecer una sola accion evidente. El servidor tiene la tabla
// completa de transiciones y es el que decide: esto es un atajo de la interfaz, no la regla.
const SIGUIENTE_ESTADO = {
	borrador: 'confirmado',
	confirmado: 'en_preparacion',
	en_preparacion: 'listo',
	listo: 'entregado',
	entregado: 'cerrado'
};

export function color_estado(estado){
	return COLOR_ESTADO[estado] || 'medium';
}

export function siguiente_estado(estado){
	return SIGUIENTE_ESTADO[estado] || '';
}

// Los valores viajan en snake_case porque asi los serializa la API. Pasarlos a texto legible en
// un solo lugar evita que cada pantalla invente el suyo.
export function etiqueta(valor){
	if(!valor) return '';
	return String(valor).replace(/_/g, ' ').replace(/^./, letra => letra.toUpperCase());
}
