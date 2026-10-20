<template>
	<comp-page titulo="Preparación" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<!-- Los contadores los calcula la API sobre el total, no sobre la página, y se tocan
			     para filtrar: el mismo patrón que pedidos y entregas. -->
			<comp-resumen-indicadores
				:items="indicadores"
				:activos="estados_activos.length > 0 ? estados_activos : ['todos']"
				@seleccionar="filtrar_estado"
			/>

			<comp-esqueleto v-if="store.cargando && !store.hay_comandas" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_comandas"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_comandas"
				:icono="icono"
				:titulo="estados_activos.length > 0 ? 'Sin resultados' : 'No hay nada para preparar'"
				mensaje="Acá aparecen los pedidos confirmados, en orden de llegada."
			/>
			<comp-lista
				v-else
				:cargar_mas="store.cargar_mas"
				:hay_mas="store.hay_mas"
				:mostrados="store.comandas.length"
				:total="store.total"
			>
				<!-- La tarjeta de la comanda: el estado que calcula la API, el pedido y para quién,
				     y abajo lo que la cocina mira: cuántos ítems salieron y cuánto lleva esperando. -->
				<ion-item v-for="comanda in store.comandas" :key="comanda.pedido_id" button lines="none" :detail="false" @click="abrir_detalle(comanda)">
					<div class="raveat-tarjeta">
						<div class="raveat-fila">
							<div class="raveat-icono-estado" :class="clase_tinte(estado_pedido(comanda.estado).color)">
								<ion-icon :icon="estado_pedido(comanda.estado).icono" aria-hidden="true" />
							</div>
							<div class="raveat-fila__texto">
								<h3 class="raveat-item-title">{{ comanda.cliente_nombre || 'Mostrador' }}</h3>
								<p class="raveat-item-meta">
									<span class="raveat-codigo raveat-tinte-primary">{{ comanda.codigo }}</span>
									{{ tipo_pedido(comanda.tipo).texto }}
								</p>
							</div>
							<div class="raveat-fila__derecha">
								<strong class="raveat-precio-destacado">{{ comanda.items_listos }} / {{ comanda.total_items }}</strong>
								<span class="raveat-pill" :class="clase_tinte(estado_pedido(comanda.estado).color)">{{ estado_pedido(comanda.estado).texto }}</span>
							</div>
						</div>
						<div class="raveat-fila__pie">
							<span class="raveat-pill raveat-tinte-medium">
								<ion-icon :icon="icono_espera" aria-hidden="true" />
								Hace {{ formatear_espera(comanda.desde_en) }}
							</span>
						</div>
					</div>
				</ion-item>
			</comp-lista>
		</div>

		<!-- La ficha dibuja y emite; la pantalla, que conoce el store, ejecuta. -->
		<comp-comanda-detalle-modal
			:abierto="detalle_abierto"
			:comanda="comanda_detalle"
			:error="store.error || ''"
			:guardando="store.guardando"
			:items="items_detalle"
			@actualizar="cargar"
			@cerrar="cerrar_detalle"
			@marcar_lista="marcar_lista"
			@mover_item="mover_item"
		/>
	</comp-page>
</template>

<script>import {
	IonIcon,
	IonItem
} from '@ionic/vue';
import { clipboardOutline, layersOutline, timerOutline } from 'ionicons/icons';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_resumen_indicadores from '@/components/base/comp_resumen_indicadores.vue';
import comp_comanda_detalle_modal from '@/components/dominio/comp_comanda_detalle_modal.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { avisar_exito } from '@/services/avisos_service';
import { clase_tinte, estado_pedido, tipo_pedido } from '@/utils/estados';
import { formatear_espera } from '@/utils/formato_fecha';
import { use_comandas_store } from '@/stores/comandas_store';

// La pantalla del rol PROCESO: lo único que necesita ver durante todo su turno.
export default {
	name: 'comandas_page',
	components: {
		CompComandaDetalleModal: comp_comanda_detalle_modal,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		CompResumenIndicadores: comp_resumen_indicadores,
		IonIcon,
		IonItem
	},
	data(){
		return {
			// La comanda abierta se guarda por id y, aparte, su última versión conocida: al marcar
			// el último ítem el pedido pasa a "listo" y, con el filtro puesto, sale de la lista. Sin
			// esa copia la ficha se vaciaba justo cuando el cocinero acababa de tocar el botón.
			comanda_id: null,
			comanda_ultima: null,
			detalle_abierto: false,
			// Los tres estados en los que un pedido pasa por cocina, en el orden en que los recorre.
			estados_disponibles: [
				'confirmado',
				'en_preparacion',
				'listo'
			],
			icono: clipboardOutline,
			icono_espera: timerOutline,
			items_ultimos: [],
			store: use_comandas_store()
		};
	},
	computed: {
		estados_activos(){
			var vm = this;
			return vm.store.filtros.estados || [];
		},
		indicadores(){
			var vm = this;
			return [
				{id: 'todos', titulo: 'Todas', valor: vm.store.total, color: 'primary', icono: layersOutline},
				...vm.estados_disponibles.map(estado => ({
					id: estado,
					titulo: estado_pedido(estado).texto,
					valor: vm.store.resumen[estado] || 0,
					color: estado_pedido(estado).color,
					icono: estado_pedido(estado).icono
				}))
			];
		},
		// Se busca en la lista viva: un refresco actualiza la ficha abierta sin volver a abrirla.
		comanda_detalle(){
			var vm = this;
			return vm.store.comandas.find(comanda => comanda.pedido_id == vm.comanda_id) || vm.comanda_ultima;
		},
		items_detalle(){
			var vm = this;
			if(!vm.comanda_id) return [];
			const items = vm.store.items_de(vm.comanda_id);
			return items.length > 0 ? items : vm.items_ultimos;
		}
	},
	// Ionic cachea la pagina: al volver a entrar no se monta de nuevo, pero este hook corre en
	// cada entrada, incluida la primera. La cocina tiene que ver siempre lo ultimo que entro.
	ionViewWillEnter(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.store.cargar();
		},
		filtrar_estado: function(id){
			var vm = this;
			if(id == 'todos') return vm.store.cargar({estados: []});
			return vm.store.alternar_estado(id);
		},
		abrir_detalle: function(comanda){
			var vm = this;
			vm.store.error = null;
			vm.comanda_id = comanda.pedido_id;
			vm.comanda_ultima = comanda;
			vm.items_ultimos = vm.store.items_de(comanda.pedido_id);
			vm.detalle_abierto = true;
		},
		cerrar_detalle: function(){
			var vm = this;
			vm.detalle_abierto = false;
			vm.comanda_id = null;
			vm.comanda_ultima = null;
			vm.items_ultimos = [];
		},
		mover_item: function(evento){
			var vm = this;
			return vm.store.mover_item(evento.item.id, evento.estado);
		},
		marcar_lista: async function(comanda){
			var vm = this;
			if(!await vm.store.marcar_lista(comanda.pedido_id)) return;
			avisar_exito(`${comanda.codigo}: todo listo.`);
			vm.cerrar_detalle();
		},
		clase_tinte,
		estado_pedido,
		formatear_espera,
		tipo_pedido
	}
};</script>
