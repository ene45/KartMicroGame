# Circuito de mazmorra — ajustes

Abrí `Assets/DungeonTrack/DungeonCircuit_Ajustes.unity`. Esta entrega incluye la escena, materiales, vistas de Unity y un generador de editor para crear otras copias. La escena usa el kart, la cámara, la interfaz y el sistema de vueltas del proyecto original. Probala abriéndola directamente y entrando en Play Mode.

## Cambios de la pista

- Mazmorra: una única sala con una envolvente de 280 × 280 m y techo a 60 m de altura. La esquina de la meta queda recortada para conservarla afuera y mantener los arcos de entrada y salida. El centro está libre para un landmark grande, con una referencia de 110 × 100 m y suelo a -4,5 m. Las paredes altas rodean la sala completa; la carretera interior tiene bordes bajos independientes.
- Zona roja: cima aproximada de 28 m y una bajada más pronunciada hasta el arco de salida, a altura 0 m. El ancho sigue siendo 18 m.
- Zona violeta: ancho de 32 m en el tramo inferior, con transición gradual a los tramos vecinos. La izquierda está a 20 m y la derecha a 0 m, siguiendo la imagen marcada. En el sentido horario de carrera se conduce de derecha a izquierda, subiendo mientras las futuras rocas pueden bajar hacia el kart.
- Zona azul: curva y recta derechas de 24 m de ancho, conservando el peralte gradual hasta 9°.
- Exterior: pasto continuo bajo la meta, la aproximación, el primer salto y el tramo de salida. La carretera exterior sigue teniendo 18 m de ancho.
- Contención: bordes con collider, de 1,4 m de altura y 0,9 m de espesor, junto a toda la carretera interior, incluidos los tres caminos y el segundo salto. Siguen las alturas y el peralte de la pista.

Se conservan los tres caminos de la entrada: el central mide 62 m y cada lateral aproximadamente 82 m. Los saltos tienen huecos iniciales de 3 m y 3,5 m. Las trampas, los boosts y la decoración del landmark quedan para la siguiente etapa.

## Editar con ProBuilder

En la jerarquía, abrí `CIRCUITO_MAZMORRA_PROBUILDER`:

- `01_Pista_Editable_18_24_32m`: tramos de carretera; editar vértices, aristas o caras con ProBuilder.
- `02_Paredes_Sala_60m`: paredes altas del recinto y dinteles sobre los portales.
- `03_Techos_Ocultar_Para_Editar`: techo único. Desactivá el grupo para ver la sala desde arriba; podés moverlo verticalmente para cambiar la altura.
- `04_Pasto_Exterior`: suelo exterior continuo.
- `05_Arcos_Entrada_Salida`: arcos independientes.
- `07_Reservas_Trampas_Y_Landmark`: referencias para el trabajo posterior.
- `08_Bordes_Contencion_1_4m`: bordes bajos, separados de la carretera y de las paredes de la sala.
- `09_Suelo_Sala_Y_Landmark`: suelo del recinto.

Son mallas estáticas de ProBuilder: el generador no corre durante la partida. La carretera tiene siete bandas a lo ancho y divisiones aproximadamente cada 2 m a lo largo. Las piezas vecinas son objetos independientes; al mover una unión, mové ambos extremos y sus bordes para conservar la continuidad.

`Tools > Dungeon Track > Crear una copia nueva del circuito` crea otra escena con nombre nuevo y conserva las escenas que editaste. La escena anterior `DungeonCircuit 4` se conserva en el proyecto local del usuario; el paquete actualizado entrega la nueva versión de ajustes.

## Validación

Generación y reapertura en Unity 6000.0.42f1 con ProBuilder 6.0.4: 114 mallas editables y 28 tramos de carretera conservados con sus colliders. Pasaron 2745 raycasts sobre la carretera, 962 contra los bordes y 213 para comprobar el pasto bajo la pista exterior. También se revisaron los triángulos, las uniones, los huecos de salto, la ubicación de la carretera dentro de la sala y las aperturas de los arcos.

`Validation.txt` conserva el resultado. En `Preview`, `Planta` y `Perspectiva` muestran la sala con el techo oculto; `Sala` y `Entrada` muestran el interior con el techo activo.

Falta probar y calibrar las pendientes y los saltos conduciendo el kart. Sus valores están preparados para modificarlos manualmente. MainScene y los scripts de conducción se conservan; la nueva escena no cambia el menú ni la lista de escenas del build.
