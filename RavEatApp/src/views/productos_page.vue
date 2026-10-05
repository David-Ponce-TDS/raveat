<template>
	<comp-page titulo="Productos" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-panel-resumen titulo="Catálogo" :subtitulo="resumen_texto">
				<ion-button expand="block" @click="abrir_nuevo">
					<ion-icon slot="start" :icon="icono_agregar" />
					Nuevo producto
				</ion-button>
			</comp-panel-resumen>

			<comp-buscador v-model="busqueda" placeholder="Buscar por nombre o descripción" @buscar="buscar" />

			<!-- Los contadores son botones: tocarlos filtra. Por eso el resumen que devuelve la API
			     NO aplica el filtro de disponibilidad — si se contara a sí mismo, al filtrar por
			     "disponibles" el número de no disponibles daría siempre 0. -->
			<comp-resumen-indicadores
				v-if="store.catalogo_resumen"
				:items="indicadores"
				:activos="[filtro_activo]"
				@seleccionar="filtrar_disponible"
			/>

			<!-- La categoria se elige tocando, sin desplegable: un filtro que hay que abrir para
			     ver que esta puesto esconde el estado de la pantalla. Filtra la API
			     (?categoria_id=), porque la lista pagina. -->
			<div class="raveat-chips">
				<button
					v-for="chip in chips_categoria"
					:key="chip.id"
					type="button"
					class="raveat-chip"
					:class="{ activo: String(chip.id) == String(categoria_activa) }"
					@click="filtrar_categoria(chip.id)"
				>
					{{ chip.nombre }}
				</button>
			</div>

			<comp-esqueleto v-if="store.catalogo_cargando && !store.catalogo_hay_productos" />
			<comp-estado-error
				v-else-if="store.error && !store.catalogo_hay_productos"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.catalogo_hay_productos"
				:icono="icono"
				:titulo="hay_filtros ? 'Sin resultados' : 'Todavía no hay productos'"
				:mensaje="hay_filtros ? 'Probá con otro texto o quitá el filtro.' : 'Cargá el primero con el botón de arriba.'"
			/>
			<comp-lista
				v-else
				:cargar_mas="store.cargar_mas_catalogo"
				:hay_mas="store.catalogo_hay_mas"
				:mostrados="store.catalogo_productos.length"
				:total="total_catalogo"
			>
				<!-- El deslizamiento alterna la DISPONIBILIDAD, que es la accion de todos los dias
				     ("se termino el flan"). La baja saca el producto de la carta, no se deshace, y
				     por eso vive dentro del formulario y no a un deslizamiento de distancia. -->
				<ion-item-sliding v-for="producto in store.catalogo_productos" :key="producto.id">
					<!-- La fila es un componente de dominio, no markup suelto: es el mismo producto
					     que dibuja la vitrina, con otra cara. -->
					<comp-producto-lista-item :producto="producto" @click="abrir_edicion(producto)" />
					<ion-item-options side="end">
						<ion-item-option
							:color="producto.disponible ? 'medium' : 'success'"
							:disabled="store.guardando"
							@click="alternar_disponible(producto, $event)"
						>
							{{ producto.disponible ? 'No disponible' : 'Disponible' }}
						</ion-item-option>
					</ion-item-options>
				</ion-item-sliding>
			</comp-lista>
		</div>

		<comp-producto-formulario-modal
			:abierto="modal_abierto"
			:error="store.error || ''"
			:guardando="store.guardando"
			:producto="producto_editando"
			@cerrar="cerrar_modal"
			@eliminar="confirmar_baja"
			@guardar="guardar"
		/>
	</comp-page>
</template>

<script>import {
	IonButton,
	IonIcon,
	IonItemOption,
	IonItemOptions,
	IonItemSliding,
	alertController
} from '@ionic/vue';
import { addOutline, checkmarkCircleOutline, eyeOffOutline, layersOutline, pricetagsOutline } from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_resumen_indicadores from '@/components/base/comp_resumen_indicadores.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_panel_resumen from '@/components/estructura/comp_panel_resumen.vue';
import comp_producto_formulario_modal from '@/components/dominio/comp_producto_formulario_modal.vue';
import comp_producto_lista_item from '@/components/dominio/comp_producto_lista_item.vue';
import { avisar_exito } from '@/services/avisos_service';
import { use_categorias_store } from '@/stores/categorias_store';
import { use_productos_store } from '@/stores/productos_store';

// El catalogo de gestion: paginado, filtrable y con ABM. La vitrina de Inicio muestra los mismos
// productos con otra cara y sin paginar; las dos leen del mismo store, en listas separadas.
export default {
	name: 'productos_page',
	components: {
		CompBuscador: comp_buscador,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		CompPanelResumen: comp_panel_resumen,
		CompProductoFormularioModal: comp_producto_formulario_modal,
		CompProductoListaItem: comp_producto_lista_item,
		CompResumenIndicadores: comp_resumen_indicadores,
		IonButton,
		IonIcon,
		IonItemOption,
		IonItemOptions,
		IonItemSliding
	},
	data(){
		return {
			busqueda: '',
			categoria_activa: 'todas',
			categorias_store: use_categorias_store(),
			disponible: null,
			icono: pricetagsOutline,
			icono_agregar: addOutline,
			modal_abierto: false,
			producto_editando: null,
			store: use_productos_store()
		};
	},
	computed: {
		hay_filtros(){
			var vm = this;
			return Boolean(vm.busqueda) || vm.disponible != null || vm.categoria_activa != 'todas';
		},
		resumen_texto(){
			var vm = this;
			const total = vm.store.catalogo_resumen ? vm.store.catalogo_resumen.total : 0;
			return `${total} ${total == 1 ? 'producto activo' : 'productos activos'}`;
		},
		// Todas las categorias activas, no solo las que tienen productos en la pagina cargada.
		chips_categoria(){
			var vm = this;
			return [
				{id: 'todas', nombre: 'Todas'},
				...vm.categorias_store.categorias.map(categoria => ({id: categoria.id, nombre: categoria.nombre}))
			];
		},
		total_catalogo(){
			var vm = this;
			return vm.store.catalogo_pagina ? vm.store.catalogo_pagina.total : 0;
		},
		// El filtro de disponibilidad tiene tres valores —null, true, false— y el indicador
		// necesita un id: se traducen aca, en un solo lugar.
		filtro_activo(){
			var vm = this;
			if(vm.disponible == null) return 'todos';
			return vm.disponible ? 'disponibles' : 'no_disponibles';
		},
		indicadores(){
			var vm = this;
			const resumen = vm.store.catalogo_resumen;
			return [
				{id: 'todos', titulo: 'Todos', valor: resumen.total, color: 'primary', icono: layersOutline},
				{id: 'disponibles', titulo: 'Disponibles', valor: resumen.disponibles, color: 'success', icono: checkmarkCircleOutline},
				{id: 'no_disponibles', titulo: 'No disponibles', valor: resumen.no_disponibles, color: 'medium', icono: eyeOffOutline}
			];
		}
	},
	mounted(){
		var vm = this;
		vm.cargar();
		vm.categorias_store.cargar_categorias();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.store.cargar_catalogo();
		},
		// La busqueda sale a la API. Filtrar aca solo miraria la pagina descargada y diria
		// "sin resultados" teniendo resultados en las paginas siguientes.
		buscar: function(texto){
			var vm = this;
			return vm.store.cargar_catalogo({busqueda: texto});
		},
		filtrar_disponible: function(id){
			var vm = this;
			vm.disponible = id == 'todos' ? null : id == 'disponibles';
			return vm.store.cargar_catalogo({disponible: vm.disponible});
		},
		filtrar_categoria: function(id){
			var vm = this;
			vm.categoria_activa = id;
			return vm.store.cargar_catalogo({categoria_id: id == 'todas' ? null : id});
		},
		// Se manda el producto entero con la disponibilidad invertida: el PUT es el mismo de la
		// edicion, y sin foto adjunta el servidor conserva la que tiene.
		alternar_disponible: async function(producto, evento){
			var vm = this;
			const deslizable = evento.target.closest('ion-item-sliding');
			if(deslizable) deslizable.close();
			const guardado = await vm.store.guardar_producto(producto.id, {
				categoria_id: producto.categoria_id,
				nombre: producto.nombre,
				descripcion: producto.descripcion,
				precio: producto.precio,
				disponible: !producto.disponible
			});
			if(guardado) avisar_exito(producto.disponible ? `"${producto.nombre}" quedó como no disponible.` : `"${producto.nombre}" vuelve a estar disponible.`);
		},
		abrir_nuevo: function(){
			var vm = this;
			vm.producto_editando = null;
			vm.store.error = null;
			vm.modal_abierto = true;
		},
		abrir_edicion: function(producto){
			var vm = this;
			vm.producto_editando = producto;
			vm.store.error = null;
			vm.modal_abierto = true;
		},
		cerrar_modal: function(){
			var vm = this;
			vm.modal_abierto = false;
			vm.producto_editando = null;
		},
		guardar: async function(evento){
			var vm = this;
			const guardado = await vm.store.guardar_producto(evento.id, evento.datos, evento.imagen);
			// El modal solo se cierra si el servidor acepto: si no, queda abierto mostrando el
			// error y sin perder lo que el usuario habia escrito.
			if(guardado){
				vm.cerrar_modal();
				avisar_exito(evento.id ? 'Producto actualizado.' : 'Producto creado.');
			}
		},
		confirmar_baja: async function(producto){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Dar de baja',
				message: `¿Dar de baja "${producto.nombre}"? Los pedidos que ya lo incluyen no se modifican.`,
				buttons: [
					{text: 'Cancelar', role: 'cancel'},
					{text: 'Dar de baja', role: 'destructive', handler: async () =>{
						if(!await vm.store.eliminar_producto_catalogo(producto.id)) return;
						vm.cerrar_modal();
						avisar_exito(`"${producto.nombre}" dado de baja.`);
					}}
				]
			});
			await alerta.present();
		}
	}
};</script>
