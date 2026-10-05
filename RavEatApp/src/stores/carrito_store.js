import { defineStore } from 'pinia';

// El borrador del pedido. Hasta la version 8 vivia en el data() del formulario, porque nacia y
// moria con el modal. Ahora se arma en la vitrina y se cierra en Pedidos: es estado compartido
// entre pantallas, y eso es lo unico que decide si algo va a Pinia. La regla no cambio; cambio el
// ciclo de vida del dato.
export const use_carrito_store = defineStore('carrito', {
	state: () => ({
		items: []
	}),
	getters: {
		hay_items(state){
			return state.items.length > 0;
		},
		cantidad_total(state){
			return state.items.reduce((total, item) => total + item.cantidad, 0);
		},
		// Es una estimacion: el total que vale lo calcula el servidor con los precios de la base.
		subtotal_estimado(state){
			return state.items.reduce((total, item) => total + item.precio * item.cantidad, 0);
		}
	},
	actions: {
		cantidad_de: function(producto_id){
			var vm = this;
			const item = vm.items.find(actual => actual.producto_id == producto_id);
			return item ? item.cantidad : 0;
		},
		// Se copian el nombre, el precio y la foto: el formulario dibuja el borrador sin volver a
		// buscar cada producto en la carta.
		agregar: function(producto){
			var vm = this;
			const item = vm.items.find(actual => actual.producto_id == producto.id);
			if(item) item.cantidad += 1;
			else vm.items.push({producto_id: producto.id, nombre: producto.nombre, precio: producto.precio, imagen_url: producto.imagen_url || '', cantidad: 1});
		},
		quitar: function(producto_id){
			var vm = this;
			const item = vm.items.find(actual => actual.producto_id == producto_id);
			if(!item) return;
			item.cantidad -= 1;
			if(item.cantidad <= 0) vm.items = vm.items.filter(actual => actual.producto_id != producto_id);
		},
		limpiar: function(){
			var vm = this;
			vm.items = [];
		}
	}
});
