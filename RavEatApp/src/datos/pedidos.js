// Datos de prueba de pedidos, con la forma que va a devolver la API. Los valores viajan en
// snake_case, igual que los serializa el backend:
//   pedido -> borrador, confirmado, en_preparacion, listo, entregado, cerrado, cancelado
//   pago   -> pendiente, pagado, anulado
//   tipo   -> presencial, retiro, entregas
// La lista cubre varios estados a proposito: con todos iguales no se veria que cada uno se pinta
// distinto. El pago vive dentro del pedido y no en una tabla aparte: es el modelo del taller.

export const PEDIDOS = [
	{
		id: 1,
		codigo: 'PED-0001',
		cliente_id: 1,
		cliente_nombre: 'María López',
		tipo: 'entregas',
		estado: 'en_preparacion',
		subtotal: 14700,
		descuento: 0,
		total: 14700,
		estado_pago: 'pendiente',
		medio_pago: null,
		propina_importe: 0,
		observaciones: 'Sin cebolla en la hamburguesa.',
		creado_en: '2026-08-01T12:15:00-03:00'
	},
	{
		id: 2,
		codigo: 'PED-0002',
		cliente_id: 2,
		cliente_nombre: 'Julián Pérez',
		tipo: 'retiro',
		estado: 'listo',
		subtotal: 8500,
		descuento: 500,
		total: 8000,
		estado_pago: 'pagado',
		medio_pago: 'transferencia',
		propina_importe: 0,
		observaciones: null,
		creado_en: '2026-08-01T12:40:00-03:00'
	},
	{
		id: 3,
		codigo: 'PED-0003',
		cliente_id: 3,
		cliente_nombre: 'Carla Fernández',
		tipo: 'presencial',
		estado: 'entregado',
		subtotal: 11700,
		descuento: 0,
		total: 11700,
		estado_pago: 'pagado',
		medio_pago: 'efectivo',
		propina_importe: 1000,
		observaciones: null,
		creado_en: '2026-08-01T13:05:00-03:00'
	},
	// Sin cliente: un pedido de mostrador no siempre tiene a quien asociarse.
	{
		id: 4,
		codigo: 'PED-0004',
		cliente_id: null,
		cliente_nombre: null,
		tipo: 'presencial',
		estado: 'confirmado',
		subtotal: 5700,
		descuento: 0,
		total: 5700,
		estado_pago: 'pendiente',
		medio_pago: null,
		propina_importe: 0,
		observaciones: null,
		creado_en: '2026-08-01T13:30:00-03:00'
	},
	{
		id: 5,
		codigo: 'PED-0005',
		cliente_id: 5,
		cliente_nombre: 'Lucía Giménez',
		tipo: 'retiro',
		estado: 'cancelado',
		subtotal: 3500,
		descuento: 0,
		total: 3500,
		estado_pago: 'anulado',
		medio_pago: null,
		propina_importe: 0,
		observaciones: 'El cliente no se presentó.',
		creado_en: '2026-08-01T13:50:00-03:00'
	}
];

// Cada estado con su color de Ionic. Vive con los datos y no en la pantalla porque mas de una
// pantalla lo va a necesitar.
export const COLOR_ESTADO_PEDIDO = {
	borrador: 'medium',
	confirmado: 'primary',
	en_preparacion: 'warning',
	listo: 'success',
	entregado: 'success',
	cerrado: 'medium',
	cancelado: 'danger'
};

// De snake_case a texto legible, en un solo lugar: si no, cada pantalla inventa el suyo.
export function etiqueta_estado(estado){
	if(!estado) return '';
	return estado.replace(/_/g, ' ').replace(/^./, letra => letra.toUpperCase());
}

const RETARDO_SIMULADO = 600;

export function obtener_pedidos(){
	return new Promise(resolver =>{
		window.setTimeout(() => resolver({pedidos: PEDIDOS}), RETARDO_SIMULADO);
	});
}
