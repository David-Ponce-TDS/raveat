<template>
	<comp-page titulo="Clientes" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-buscador v-model="busqueda" placeholder="Buscar por nombre, teléfono o email" @buscar="buscar" />

			<ion-button expand="block" fill="outline" size="small" @click="abrir_nuevo">
				<ion-icon slot="start" :icon="icono_agregar" />
				Nuevo cliente
			</ion-button>

			<comp-esqueleto v-if="clientes_store.cargando && !clientes_store.hay_clientes" />
			<comp-estado-error
				v-else-if="clientes_store.error && !clientes_store.hay_clientes"
				:mensaje="clientes_store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!clientes_store.hay_clientes"
				:icono="icono"
				:titulo="busqueda ? 'Sin resultados' : 'Todavía no hay clientes'"
				:mensaje="busqueda ? 'Probá con otro texto.' : 'Cargá el primero con el botón de arriba.'"
			/>
			<comp-lista
				v-else
				:cargando="clientes_store.cargando"
				:hay_mas="clientes_store.hay_mas"
				:mostrados="clientes_store.clientes.length"
				:total="clientes_store.total"
				@cargar_mas="clientes_store.cargar_mas()"
			>
				<ion-item-sliding v-for="cliente in clientes_store.clientes" :key="cliente.id">
					<ion-item button @click="abrir_edicion(cliente)">
						<ion-avatar slot="start" class="raveat-avatar-inicial">
							<span>{{ inicial(cliente.nombre) }}</span>
						</ion-avatar>
						<ion-label>
							<h3 class="raveat-item-title">{{ cliente.nombre }}</h3>
							<p>{{ cliente.telefono }}</p>
							<p class="raveat-item-meta">{{ cliente.direccion_linea || 'Sin dirección · solo retiro' }}</p>
						</ion-label>
					</ion-item>
					<ion-item-options side="end">
						<ion-item-option color="danger" @click="confirmar_baja(cliente)">Baja</ion-item-option>
					</ion-item-options>
				</ion-item-sliding>
			</comp-lista>
		</div>

		<comp-cliente-formulario-modal
			:abierto="modal_abierto"
			:cliente="cliente_editando"
			:error="clientes_store.error || ''"
			:guardando="clientes_store.guardando"
			@cerrar="cerrar_modal"
			@guardar="guardar"
		/>
	</comp-page>
</template>

<script>import {
	IonAvatar,
	IonButton,
	IonIcon,
	IonItem,
	IonItemOption,
	IonItemOptions,
	IonItemSliding,
	IonLabel,
	alertController
} from '@ionic/vue';
import { addOutline, peopleOutline } from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_cliente_formulario_modal from '@/components/dominio/comp_cliente_formulario_modal.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { use_clientes_store } from '@/stores/clientes_store';

export default {
	name: 'clientes_page',
	components: {
		CompBuscador: comp_buscador,
		CompClienteFormularioModal: comp_cliente_formulario_modal,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		IonAvatar,
		IonButton,
		IonIcon,
		IonItem,
		IonItemOption,
		IonItemOptions,
		IonItemSliding,
		IonLabel
	},
	data(){
		return {
			busqueda: '',
			cliente_editando: null,
			clientes_store: use_clientes_store(),
			icono: peopleOutline,
			icono_agregar: addOutline,
			modal_abierto: false
		};
	},
	mounted(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.clientes_store.cargar();
		},
		// La busqueda sale a la API. Filtrar acá solo miraría la página descargada y diría
		// "sin resultados" teniendo resultados en las páginas siguientes.
		buscar: function(texto){
			var vm = this;
			return vm.clientes_store.cargar({busqueda: texto});
		},
		abrir_nuevo: function(){
			var vm = this;
			vm.cliente_editando = null;
			vm.clientes_store.error = null;
			vm.modal_abierto = true;
		},
		abrir_edicion: function(cliente){
			var vm = this;
			vm.cliente_editando = cliente;
			vm.clientes_store.error = null;
			vm.modal_abierto = true;
		},
		cerrar_modal: function(){
			var vm = this;
			vm.modal_abierto = false;
			vm.cliente_editando = null;
		},
		guardar: async function(evento){
			var vm = this;
			const guardado = await vm.clientes_store.guardar(evento.id, evento.datos);
			// El modal solo se cierra si el servidor acepto: si no, queda abierto mostrando el
			// error y sin perder lo que el usuario habia escrito.
			if(guardado) vm.cerrar_modal();
		},
		confirmar_baja: async function(cliente){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Dar de baja',
				message: `¿Dar de baja a ${cliente.nombre}?`,
				buttons: [
					{text: 'Cancelar', role: 'cancel'},
					{text: 'Dar de baja', role: 'destructive', handler: () => vm.clientes_store.eliminar(cliente.id)}
				]
			});
			await alerta.present();
		},
		inicial: function(nombre){
			return (nombre || '?').trim().charAt(0).toUpperCase();
		}
	}
};</script>
