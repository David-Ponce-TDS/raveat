<template>
	<comp-page titulo="Entregas" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<!-- Contadores por estado: los calcula la API sobre el total filtrado, no sobre la
			     página. Se pueden combinar, igual que en pedidos; "Todas" limpia la selección. -->
			<comp-resumen-indicadores
				:items="indicadores"
				:activos="estados_activos.length > 0 ? estados_activos : ['todos']"
				@seleccionar="filtrar_estado"
			/>

			<!-- "Mis entregas" filtra con un flag, no mandando un id: la identidad sale del token.
			     Para delivery arranca prendido — su pantalla de todo el día son las suyas. -->
			<ion-item lines="none" class="raveat-card">
				<ion-label>Mis entregas y las pendientes</ion-label>
				<ion-toggle slot="end" :checked="store.filtros.solo_mias" @ion-change="cambiar_solo_mias" />
			</ion-item>

			<comp-esqueleto v-if="store.cargando && !store.hay_entregas" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_entregas"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_entregas"
				:icono="icono"
				:titulo="hay_filtros ? 'Sin resultados' : 'Todavía no hay entregas'"
				mensaje="Las entregas nacen con los pedidos de tipo entrega."
			/>
			<comp-lista
				v-else
				:cargar_mas="store.cargar_mas"
				:hay_mas="store.hay_mas"
				:mostrados="store.entregas.length"
				:total="store.total"
			>
				<!-- La tarjeta de la entrega: el icono del estado, la direccion como titulo (es lo
				     que el repartidor busca), el pedido y el importe a la derecha, y abajo quien la
				     lleva. Tocarla abre la ficha. -->
				<ion-item v-for="entrega in store.entregas" :key="entrega.id" button lines="none" :detail="false" @click="abrir_detalle(entrega)">
					<div class="raveat-tarjeta">
						<div class="raveat-fila">
							<div class="raveat-icono-estado" :class="clase_tinte(estado_entrega(entrega.estado).color)">
								<ion-icon :icon="estado_entrega(entrega.estado).icono" aria-hidden="true" />
							</div>
							<div class="raveat-fila__texto">
								<h3 class="raveat-item-title">{{ entrega.direccion_linea }}</h3>
								<p class="raveat-item-meta">
									<span class="raveat-codigo raveat-tinte-primary">{{ entrega.pedido_codigo }}</span>
									{{ entrega.cliente_nombre || 'Sin cliente' }}
								</p>
							</div>
							<div class="raveat-fila__derecha">
								<strong class="raveat-precio-destacado">{{ formatear_importe(entrega.pedido_total) }}</strong>
								<span class="raveat-pill" :class="clase_tinte(estado_entrega(entrega.estado).color)">{{ estado_entrega(entrega.estado).texto }}</span>
							</div>
						</div>
						<div class="raveat-fila__pie">
							<span v-if="entrega.repartidor_nombre" class="raveat-pill raveat-tinte-tertiary">
								<ion-icon :icon="icono_repartidor" aria-hidden="true" />
								Lleva {{ entrega.repartidor_nombre }}
							</span>
							<span v-else class="raveat-pill raveat-tinte-medium">
								<ion-icon :icon="icono_sin_repartidor" aria-hidden="true" />
								Sin repartidor
							</span>
							<!-- El mapa a un toque desde la fila: en la calle, abrir la ficha para
							     buscar la direccion es un paso de mas. El .stop evita abrir la ficha. -->
							<ion-button
								class="raveat-fila__accion"
								fill="outline"
								size="small"
								aria-label="Ver en el mapa"
								@click.stop="abrir_mapa(entrega)"
							>
								<ion-icon slot="icon-only" :icon="icono_mapa" />
							</ion-button>
						</div>
					</div>
				</ion-item>
			</comp-lista>
		</div>

		<!-- La ficha dibuja y emite; la pantalla, que conoce el store y el rol, ejecuta. -->
		<comp-entrega-detalle-modal
			:abierto="detalle_abierto"
			:entrega="entrega_detalle"
			:error="store.error || ''"
			:guardando="store.guardando"
			:puede_asignar="puede_asignar"
			:puede_firmar="puede_firmar"
			:puede_tomar="puede_tomar"
			:repartidores="repartidores"
			@asignar="asignar"
			@cancelar="confirmar_cancelacion"
			@cerrar="cerrar_detalle"
			@estado="cambiar_estado"
			@firmar="firmar"
			@tomar="tomar"
		/>
	</comp-page>
</template>

<script>import {
	IonButton,
	IonIcon,
	IonItem,
	IonLabel,
	IonToggle,
	alertController
} from '@ionic/vue';
import { bicycleOutline, layersOutline, mapOutline, personOutline, personRemoveOutline } from 'ionicons/icons';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_resumen_indicadores from '@/components/base/comp_resumen_indicadores.vue';
import comp_entrega_detalle_modal from '@/components/dominio/comp_entrega_detalle_modal.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { avisar_exito } from '@/services/avisos_service';
import { url_mapa } from '@/services/geolocalizacion_service';
import { obtener_usuarios } from '@/services/usuarios_service';
import { clase_tinte, estado_entrega } from '@/utils/estados';
import { formatear_importe } from '@/utils/formato_moneda';
import { use_entregas_store } from '@/stores/entregas_store';
import { use_sesion_store } from '@/stores/sesion_store';

export default {
	name: 'entregas_page',
	components: {
		CompEntregaDetalleModal: comp_entrega_detalle_modal,
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		CompResumenIndicadores: comp_resumen_indicadores,
		IonButton,
		IonIcon,
		IonItem,
		IonLabel,
		IonToggle
	},
	data(){
		return {
			detalle_abierto: false,
			entrega_detalle: null,
			estados_disponibles: [
				'pendiente',
				'asignada',
				'en_camino',
				'entregada',
				'cancelada'
			],
			icono: bicycleOutline,
			icono_mapa: mapOutline,
			icono_repartidor: personOutline,
			icono_sin_repartidor: personRemoveOutline,
			// Los candidatos a repartidor. Solo los carga el admin: el listado de usuarios es suyo.
			repartidores: [],
			sesion_store: use_sesion_store(),
			store: use_entregas_store()
		};
	},
	computed: {
		estados_activos(){
			var vm = this;
			return vm.store.filtros.estados || [];
		},
		hay_filtros(){
			var vm = this;
			return vm.estados_activos.length > 0 || vm.store.filtros.solo_mias;
		},
		indicadores(){
			var vm = this;
			return [
				{id: 'todos', titulo: 'Todas', valor: vm.store.total, color: 'primary', icono: layersOutline},
				...vm.estados_disponibles.map(estado => ({
					id: estado,
					titulo: estado_entrega(estado).texto,
					valor: vm.store.resumen[estado] || 0,
					color: estado_entrega(estado).color,
					icono: estado_entrega(estado).icono
				}))
			];
		},
		es_delivery(){
			var vm = this;
			// El rol se compara en el idioma de la app (`delivery`), que es lo que devuelve el
			// store: mientras publicaba el codigo de la API (`DELIVERY`) esto daba siempre falso
			// y el repartidor no veia ni sus entregas ni el boton de tomarlas.
			return vm.sesion_store.rol_activo == 'delivery';
		},
		es_admin(){
			var vm = this;
			return vm.sesion_store.rol_activo == 'administrador';
		},
		// Tomarla es de delivery (y admin), y solo si esta pendiente: el servidor valida el rol
		// del asignado, esto solo evita ofrecer lo que va a ser rechazado.
		puede_tomar(){
			var vm = this;
			if(!vm.entrega_detalle) return false;
			return vm.entrega_detalle.estado == 'pendiente' && (vm.es_delivery || vm.es_admin);
		},
		// Firma el repartidor que la lleva (o el admin), con la entrega en camino o ya entregada:
		// antes no hay a quién pedirle conformidad. El servidor valida lo mismo.
		puede_firmar(){
			var vm = this;
			if(!vm.entrega_detalle) return false;
			const en_destino = vm.entrega_detalle.estado == 'en_camino' || vm.entrega_detalle.estado == 'entregada';
			const es_suya = Boolean(vm.sesion_store.usuario) && vm.entrega_detalle.repartidor_id == vm.sesion_store.usuario.id;
			return en_destino && (es_suya || vm.es_admin);
		},
		puede_asignar(){
			var vm = this;
			if(!vm.entrega_detalle) return false;
			return vm.entrega_detalle.estado == 'pendiente' && vm.es_admin;
		}
	},
	mounted(){
		var vm = this;
		// El repartidor arranca viendo lo suyo; los demas, todo.
		vm.store.cargar({solo_mias: vm.es_delivery});
		if(vm.es_admin) vm.cargar_repartidores();
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
		cambiar_solo_mias: function(evento){
			var vm = this;
			if(evento.detail.checked == vm.store.filtros.solo_mias) return;
			return vm.store.cargar({solo_mias: evento.detail.checked});
		},
		abrir_detalle: function(entrega){
			var vm = this;
			vm.store.error = null;
			vm.entrega_detalle = entrega;
			vm.detalle_abierto = true;
		},
		cerrar_detalle: function(){
			var vm = this;
			vm.detalle_abierto = false;
			vm.entrega_detalle = null;
		},
		// Despues de cada accion el store recarga la lista: la ficha se actualiza con la fila
		// nueva, que es la misma entrega con su estado nuevo.
		refrescar_detalle: function(){
			var vm = this;
			if(!vm.entrega_detalle) return;
			vm.entrega_detalle = vm.store.entregas.find(entrega => entrega.id == vm.entrega_detalle.id) || null;
			if(!vm.entrega_detalle) vm.detalle_abierto = false;
		},
		// Una sola pagina grande alcanza: son los usuarios del local, no un listado que crezca.
		// Si falla, el selector queda vacio y lo dice; la entrega se puede tomar igual.
		cargar_repartidores: async function(){
			var vm = this;
			try{
				const respuesta = await obtener_usuarios({tamano: 100});
				vm.repartidores = ((respuesta && respuesta.usuarios) || []).filter(usuario => usuario.rol_codigo == 'DELIVERY');
			}catch{
				vm.repartidores = [];
			}
		},
		asignar: async function(repartidor_id){
			var vm = this;
			const entrega = vm.entrega_detalle;
			const repartidor = vm.repartidores.find(usuario => usuario.id == repartidor_id);
			if(!await vm.store.asignar(entrega.id, repartidor_id)) return;
			vm.refrescar_detalle();
			avisar_exito(`${entrega.pedido_codigo}: la lleva ${repartidor ? repartidor.nombre : 'el repartidor elegido'}.`);
		},
		tomar: async function(){
			var vm = this;
			const entrega = vm.entrega_detalle;
			if(!await vm.store.asignar(entrega.id)) return;
			vm.refrescar_detalle();
			avisar_exito(`Tomaste la entrega de ${entrega.pedido_codigo}.`);
		},
		cambiar_estado: async function(estado){
			var vm = this;
			const entrega = vm.entrega_detalle;
			if(!await vm.store.cambiar_estado(entrega.id, estado)) return;
			vm.refrescar_detalle();
			avisar_exito(`${entrega.pedido_codigo}: ${estado_entrega(estado).texto.toLowerCase()}.`);
		},
		firmar: async function(archivo){
			var vm = this;
			const entrega = vm.entrega_detalle;
			if(!await vm.store.guardar_firma(entrega.id, archivo)) return;
			vm.refrescar_detalle();
			avisar_exito(`Firma de ${entrega.pedido_codigo} guardada.`);
		},
		confirmar_cancelacion: async function(){
			var vm = this;
			const entrega = vm.entrega_detalle;
			const alerta = await alertController.create({
				header: 'Cancelar entrega',
				message: `¿Cancelar la entrega de ${entrega.pedido_codigo}? El pedido no se cancela solo.`,
				buttons: [
					{text: 'No', role: 'cancel'},
					{text: 'Cancelar entrega', role: 'destructive', handler: async () =>{
						if(!await vm.store.cambiar_estado(entrega.id, 'cancelada')) return;
						vm.refrescar_detalle();
						avisar_exito(`Entrega de ${entrega.pedido_codigo} cancelada.`);
					}}
				]
			});
			await alerta.present();
		},
		abrir_mapa: function(entrega){
			window.open(url_mapa(entrega.direccion_latitud, entrega.direccion_longitud, entrega.direccion_linea), '_blank');
		},
		clase_tinte,
		estado_entrega,
		formatear_importe
	}
};</script>
