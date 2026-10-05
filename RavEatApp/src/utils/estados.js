// Como se PINTA cada estado de la app: texto, color de la paleta e icono, por dominio. Toda
// pantalla que dibuja un estado lee de aca y ninguna inventa su propia tabla: el mismo tinte
// significa lo mismo en la lista, en la ficha y en el resumen. Las claves llegan en snake_case,
// tal como las serializa la API. El servidor sigue siendo el dueno de las transiciones.
import {
	bagHandleOutline,
	banOutline,
	bicycleOutline,
	cardOutline,
	cashOutline,
	checkboxOutline,
	checkmarkCircleOutline,
	closeCircleOutline,
	documentTextOutline,
	flameOutline,
	informationCircleOutline,
	lockClosedOutline,
	personOutline,
	receiptOutline,
	storefrontOutline,
	swapHorizontalOutline,
	timeOutline
} from 'ionicons/icons';

const ESTADO_PEDIDO = {
	borrador: {texto: 'Borrador', color: 'medium', icono: documentTextOutline},
	confirmado: {texto: 'Confirmado', color: 'primary', icono: checkmarkCircleOutline},
	en_preparacion: {texto: 'En preparación', color: 'warning', icono: flameOutline},
	listo: {texto: 'Listo', color: 'success', icono: checkboxOutline},
	entregado: {texto: 'Entregado', color: 'tertiary', icono: bicycleOutline},
	cerrado: {texto: 'Cerrado', color: 'medium', icono: lockClosedOutline},
	cancelado: {texto: 'Cancelado', color: 'danger', icono: closeCircleOutline}
};

// El siguiente paso evidente, para ofrecer una sola accion. El servidor tiene la tabla completa
// de transiciones y es el que decide: esto es un atajo de la interfaz, no la regla.
const SIGUIENTE_ESTADO_PEDIDO = {
	borrador: 'confirmado',
	confirmado: 'en_preparacion',
	en_preparacion: 'listo',
	listo: 'entregado',
	entregado: 'cerrado'
};

const TIPO_PEDIDO = {
	presencial: {texto: 'Presencial', color: 'medium', icono: storefrontOutline},
	retiro: {texto: 'Retiro', color: 'medium', icono: bagHandleOutline},
	entregas: {texto: 'Entrega', color: 'medium', icono: bicycleOutline}
};

const ESTADO_PAGO = {
	pendiente: {texto: 'Pago pendiente', color: 'warning', icono: timeOutline},
	pagado: {texto: 'Pagado', color: 'success', icono: cashOutline},
	anulado: {texto: 'Pago anulado', color: 'medium', icono: banOutline}
};

const MEDIO_PAGO = {
	efectivo: {texto: 'Efectivo', color: 'success', icono: cashOutline},
	transferencia: {texto: 'Transferencia', color: 'primary', icono: swapHorizontalOutline},
	tarjeta: {texto: 'Tarjeta', color: 'primary', icono: cardOutline}
};

const ESTADO_ENTREGA = {
	pendiente: {texto: 'Pendiente', color: 'medium', icono: timeOutline},
	asignada: {texto: 'Asignada', color: 'primary', icono: personOutline},
	en_camino: {texto: 'En camino', color: 'warning', icono: bicycleOutline},
	entregada: {texto: 'Entregada', color: 'success', icono: checkmarkCircleOutline},
	cancelada: {texto: 'Cancelada', color: 'danger', icono: closeCircleOutline}
};

const SIGUIENTE_ESTADO_ENTREGA = {
	asignada: 'en_camino',
	en_camino: 'entregada'
};

const TIPO_NOTIFICACION = {
	pedido: {texto: 'Pedido', color: 'primary', icono: receiptOutline},
	entrega: {texto: 'Entrega', color: 'tertiary', icono: bicycleOutline},
	informacion: {texto: 'Información', color: 'medium', icono: informationCircleOutline}
};

// Un valor que la app todavia no conoce no rompe la pantalla: se muestra tal cual, en gris.
function leer(tabla, clave){
	return tabla[clave] || {texto: String(clave || ''), color: 'medium', icono: informationCircleOutline};
}

export function estado_pedido(estado){
	return leer(ESTADO_PEDIDO, estado);
}

export function siguiente_estado_pedido(estado){
	return SIGUIENTE_ESTADO_PEDIDO[estado] || '';
}

export function tipo_pedido(tipo){
	return leer(TIPO_PEDIDO, tipo);
}

export function estado_pago(estado){
	return leer(ESTADO_PAGO, estado);
}

export function medio_pago(medio){
	return leer(MEDIO_PAGO, medio);
}

export function estado_entrega(estado){
	return leer(ESTADO_ENTREGA, estado);
}

export function siguiente_estado_entrega(estado){
	return SIGUIENTE_ESTADO_ENTREGA[estado] || '';
}

export function tipo_notificacion(tipo){
	return leer(TIPO_NOTIFICACION, tipo);
}

// La clase de tinte que corresponde a un color de la paleta: `raveat-tinte-success`.
export function clase_tinte(color){
	return `raveat-tinte-${color || 'medium'}`;
}
