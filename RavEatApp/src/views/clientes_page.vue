<template>
	<comp-page titulo="Clientes" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-panel-resumen titulo="Clientes" :subtitulo="resumen_texto">
				<ion-button expand="block" @click="abrir_nuevo">
					<ion-icon slot="start" :icon="icono_agregar" />
					Nuevo cliente
				</ion-button>
			</comp-panel-resumen>

			<comp-buscador v-model="busqueda" placeholder="Buscar por nombre, teléfono o email" @buscar="buscar" />

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
				:cargar_mas="clientes_store.cargar_mas"
				:hay_mas="clientes_store.hay_mas"
				:mostrados="clientes_store.clientes.length"
				:total="clientes_store.total"
			>
				<!-- La tarjeta del cliente: la inicial, el nombre, el telefono y, como referencia,
				     a donde se le entrega. Sin direccion, la pildora dice que es solo retiro.
				     Tocarla abre su ficha; editar se hace desde ahi. -->
				<ion-item-sliding v-for="cliente in clientes_store.clientes" :key="cliente.id">
					<ion-item button lines="none" :detail="false" @click="abrir_detalle(cliente)">
						<div class="raveat-fila">
							<div class="raveat-avatar-inicial">{{ inicial(cliente.nombre) }}</div>
							<div class="raveat-fila__texto">
								<h3 class="raveat-item-title">{{ cliente.nombre }}</h3>
								<p class="raveat-item-meta">{{ cliente.telefono }}<template v-if="cliente.email"> · {{ cliente.email }}</template></p>
								<div class="raveat-fila__referencias">
									<span v-if="cliente.direccion_linea" class="raveat-pill raveat-tinte-primary">
										<ion-icon :icon="icono_direccion" aria-hidden="true" />
										{{ cliente.direccion_linea }}
									</span>
									<span v-else class="raveat-pill raveat-tinte-medium">
										<ion-icon :icon="icono_retiro" aria-hidden="true" />
										Solo retiro
									</span>
								</div>
							</div>
						</div>
					</ion-item>
					<ion-item-options side="end">
						<ion-item-option color="danger" @click="confirmar_baja(cliente)">Baja</ion-item-option>
					</ion-item-options>
				</ion-item-sliding>
			</comp-lista>
		</div>

		<comp-cliente-detalle-modal
			:abierto="detalle_abierto"
			:cliente="cliente_detalle"
			:pedidos="pedidos_cliente"
			:pedidos_cargando="pedidos_cargando"
			:pedidos_total="pedidos_total"
			@cerrar="cerrar_detalle"
			@editar="editar_desde_detalle"
			@ver_pedido="ver_pedido"
		/>

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
	IonButton,
	IonIcon,
	IonItem,
	IonItemOption,
	IonItemOptions,
	IonItemSliding,
	alertController
} from '@ionic/vue';
import { addOutline, bagHandleOutline, locationOutline, peopleOutline } from 'ionicons/icons';
import comp_buscador from '@/components/base/comp_buscador.vue';
import comp_cliente_detalle_modal from '@/components/dominio/comp_cliente_detalle_modal.vue';
import comp_cliente_formulario_modal from '@/components/dominio/comp_cliente_formulario_modal.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import comp_panel_resumen from '@/components/estructura/comp_panel_resumen.vue';
import { avisar_exito } from '@/services/avisos_service';
import { obtener_pedidos } from '@/services/pedidos_service';
import { use_clientes_store } from '@/stores/clientes_store';

export default {
	name: 'clientes_page',
	components: {
		CompBuscador: comp_buscador,
		CompClienteDetalleModal: comp_cliente_detalle_modal,
		CompClienteFormularioModal: comp_cliente_formulario_modal,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		CompPanelResumen: comp_panel_resumen,
		IonButton,
		IonIcon,
		IonItem,
		IonItemOption,
		IonItemOptions,
		IonItemSliding
	},
	data(){
		return {
			busqueda: '',
			cliente_detalle: null,
			cliente_editando: null,
			clientes_store: use_clientes_store(),
			detalle_abierto: false,
			icono: peopleOutline,
			icono_agregar: addOutline,
			icono_direccion: locationOutline,
			icono_retiro: bagHandleOutline,
			modal_abierto: false,
			pedidos_cargando: false,
			pedidos_cliente: [],
			pedidos_total: 0
		};
	},
	computed: {
		resumen_texto(){
			var vm = this;
			const total = vm.clientes_store.total;
			return `${total} ${total == 1 ? 'cliente registrado' : 'clientes registrados'}`;
		}
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
		// La ficha se abre con lo que ya trae la lista y despues pide sus pedidos: si esperara a
		// la API, se abriria vacia.
		abrir_detalle: async function(cliente){
			var vm = this;
			vm.cliente_detalle = cliente;
			vm.pedidos_cliente = [];
			vm.pedidos_total = 0;
			vm.detalle_abierto = true;
			vm.pedidos_cargando = true;
			try{
				const respuesta = await obtener_pedidos({cliente_id: cliente.id, tamano: 10});
				vm.pedidos_cliente = (respuesta && respuesta.pedidos) || [];
				vm.pedidos_total = (respuesta && respuesta.total) || vm.pedidos_cliente.length;
			}catch{
				// La ficha sigue sirviendo sin el historial: los datos del cliente ya estan.
				vm.pedidos_cliente = [];
			}finally{
				vm.pedidos_cargando = false;
			}
		},
		cerrar_detalle: function(){
			var vm = this;
			vm.detalle_abierto = false;
			vm.cliente_detalle = null;
		},
		editar_desde_detalle: function(cliente){
			var vm = this;
			vm.cerrar_detalle();
			vm.abrir_edicion(cliente);
		},
		// La ficha del pedido vive en Pedidos: se llega con el id en la query, que esa pantalla
		// ya entiende.
		ver_pedido: function(pedido){
			var vm = this;
			vm.cerrar_detalle();
			vm.$router.push({path: '/app/pedidos', query: {pedido_id: pedido.id}});
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
			if(guardado){
				vm.cerrar_modal();
				avisar_exito(evento.id ? 'Cliente actualizado.' : 'Cliente creado.');
			}
		},
		confirmar_baja: async function(cliente){
			var vm = this;
			const alerta = await alertController.create({
				header: 'Dar de baja',
				message: `¿Dar de baja a ${cliente.nombre}?`,
				buttons: [
					{text: 'Cancelar', role: 'cancel'},
					{text: 'Dar de baja', role: 'destructive', handler: async () =>{
						if(await vm.clientes_store.eliminar(cliente.id)) avisar_exito(`${cliente.nombre} dado de baja.`);
					}}
				]
			});
			await alerta.present();
		},
		inicial: function(nombre){
			return (nombre || '?').trim().charAt(0).toUpperCase();
		}
	}
};</script>
