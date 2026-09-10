// Datos de prueba de la carta. Mientras no exista la API, esta es la fuente de los productos.
//
// La forma es EXACTAMENTE la que despues devuelve `GET /api/productos/resumen` en v3:
// mismos nombres de campo, en snake_case, y las mismas tres claves de la respuesta
// ({resumen, categorias, productos}). Eso es lo que hace que pasar a la API en v3 sea cambiar
// de donde sale el dato y nada mas: ninguna pantalla se entera.
//
// Las imagenes viven en `public/seed/productos/` y se referencian con la misma ruta que usara
// la API (`/seed/productos/x.jpg`). En v3 esa ruta se resuelve contra el servidor; aca la
// sirve el propio bundle.

export const CATEGORIAS = [
	{id: 1, nombre: 'Entradas', orden: 1, cantidad: 3},
	{id: 2, nombre: 'Hamburguesas', orden: 2, cantidad: 3},
	{id: 3, nombre: 'Pizzas', orden: 3, cantidad: 3},
	{id: 4, nombre: 'Bebidas', orden: 4, cantidad: 3},
	{id: 5, nombre: 'Postres', orden: 5, cantidad: 2}
];

export const PRODUCTOS = [
	{id: 1, nombre: 'Papas fritas', descripcion: 'Porción de papas crocantes con cheddar.', precio: 3500, imagen_url: '/seed/productos/papas_fritas.jpg', disponible: true, categoria_id: 1, categoria_nombre: 'Entradas'},
	{id: 2, nombre: 'Empanadas de carne', descripcion: 'Seis empanadas caseras al horno.', precio: 4200, imagen_url: '/seed/productos/empanadas.jpg', disponible: true, categoria_id: 1, categoria_nombre: 'Entradas'},
	{id: 3, nombre: 'Provoleta', descripcion: 'Provolone grillado con orégano y aceite de oliva.', precio: 3900, imagen_url: '/seed/productos/provoleta.jpg', disponible: true, categoria_id: 1, categoria_nombre: 'Entradas'},
	{id: 4, nombre: 'Hamburguesa clásica', descripcion: 'Medallón de carne, lechuga, tomate y salsa de la casa.', precio: 6500, imagen_url: '/seed/productos/hamburguesa_clasica.jpg', disponible: true, categoria_id: 2, categoria_nombre: 'Hamburguesas'},
	{id: 5, nombre: 'Doble cheddar', descripcion: 'Doble medallón, doble cheddar y panceta.', precio: 8200, imagen_url: '/seed/productos/hamburguesa_cheddar.jpg', disponible: true, categoria_id: 2, categoria_nombre: 'Hamburguesas'},
	// El unico no disponible: sirve para ver en pantalla que "dado de baja" y "hoy no se puede
	// pedir" son dos cosas distintas.
	{id: 6, nombre: 'Veggie', descripcion: 'Medallón de garbanzos y vegetales grillados.', precio: 6200, imagen_url: '/seed/productos/hamburguesa_veggie.jpg', disponible: false, categoria_id: 2, categoria_nombre: 'Hamburguesas'},
	{id: 7, nombre: 'Muzzarella', descripcion: 'Salsa de tomate y abundante muzzarella.', precio: 7000, imagen_url: '/seed/productos/pizza_muzzarella.jpg', disponible: true, categoria_id: 3, categoria_nombre: 'Pizzas'},
	{id: 8, nombre: 'Napolitana', descripcion: 'Muzzarella, tomate fresco y ajo.', precio: 7800, imagen_url: '/seed/productos/pizza_napolitana.jpg', disponible: true, categoria_id: 3, categoria_nombre: 'Pizzas'},
	{id: 9, nombre: 'Especial', descripcion: 'Muzzarella, jamón, morrones y aceitunas.', precio: 8500, imagen_url: '/seed/productos/pizza_especial.jpg', disponible: true, categoria_id: 3, categoria_nombre: 'Pizzas'},
	{id: 10, nombre: 'Gaseosa', descripcion: 'Botella de gaseosa 500ml.', precio: 2200, imagen_url: '/seed/productos/gaseosa.jpg', disponible: true, categoria_id: 4, categoria_nombre: 'Bebidas'},
	{id: 11, nombre: 'Agua mineral', descripcion: 'Botella 500ml con o sin gas.', precio: 1800, imagen_url: '/seed/productos/agua.jpg', disponible: true, categoria_id: 4, categoria_nombre: 'Bebidas'},
	{id: 12, nombre: 'Cerveza artesanal', descripcion: 'Pinta de cerveza rubia artesanal.', precio: 3800, imagen_url: '/seed/productos/cerveza.jpg', disponible: true, categoria_id: 4, categoria_nombre: 'Bebidas'},
	{id: 13, nombre: 'Brownie con helado', descripcion: 'Brownie tibio con helado de crema.', precio: 4200, imagen_url: '/seed/productos/brownie.jpg', disponible: true, categoria_id: 5, categoria_nombre: 'Postres'},
	{id: 14, nombre: 'Flan casero', descripcion: 'Flan con dulce de leche y crema.', precio: 3500, imagen_url: '/seed/productos/flan.jpg', disponible: true, categoria_id: 5, categoria_nombre: 'Postres'}
];

// El retardo no es decorativo: sin el, los datos llegan en el mismo tick y el esqueleto de carga
// no se ve nunca. Con la API real la espera existe de verdad, asi que conviene que la pantalla
// se haya escrito contemplandola desde el primer dia.
const RETARDO_SIMULADO = 600;

export function obtener_carta(){
	const total = PRODUCTOS.length;
	const disponibles = PRODUCTOS.filter(producto => producto.disponible).length;
	return new Promise(resolver =>{
		window.setTimeout(() => resolver({
			resumen: {total, disponibles, no_disponibles: total - disponibles},
			categorias: CATEGORIAS,
			productos: PRODUCTOS
		}), RETARDO_SIMULADO);
	});
}
