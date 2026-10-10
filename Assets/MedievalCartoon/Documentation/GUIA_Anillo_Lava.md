# Anillo, lava, cadenas y guardianes grises

El nuevo conjunto usa piedra gris con un matiz frío, lava naranja y hierro oscuro, dentro del estilo medieval cartoon. Los assets anteriores siguen disponibles.

## Encontrarlos en Unity

1. Abrí el proyecto KartMicroGame. Si los nuevos archivos todavía no aparecen, ejecutá **Assets → Refresh** y esperá a que termine de compilar.
2. Entrá en **Tools → Assets de Mazmorra → Abrir biblioteca**.
3. Usá **Mostrar anillo, cadenas y 4 guardianes**. También podés abrir la carpeta **Assets/MedievalCartoon/Prefabs/Anillo_Y_Guardianes** desde Project. Borrá el texto del buscador de Project si tenés un filtro activo.
4. Arrastrá cualquier prefab a Scene o Hierarchy. Seleccionalo y pulsá **F** en Scene para enfocarlo.

La biblioteca tiene botones para abrir el catálogo y el circuito nuevo. Al cambiar de escena, Unity permite guardar la que estés editando.

## Abrir la pista de ejemplo

**Tools → Assets de Mazmorra → Abrir biblioteca → Abrir circuito con anillo de lava** abre **Assets/DungeonTrack/DungeonCircuit_AnilloLava.unity**.

Es una copia del circuito decorado, con las trampas por vuelta y las rocas. Dentro de **CIRCUITO_MAZMORRA_PROBUILDER → 11_Anillo_Lava_Y_Guardianes_Grises** están el centro y los cuatro guardianes. La pista conserva sus mallas ProBuilder.

Los guardianes de la escena están al 70% de su tamaño de prefab. El primero está en la esquina exterior junto al regreso a meta; los otros tres están dentro de la mazmorra. Sus bases se elevan donde hace falta para superar los bordes de la pista. Cada uno incluye una luz suave de lectura, editable desde su hijo **Luz_Para_Leer_Silueta**.

## Añadirlos a tu pista ya editada

Con tu circuito abierto, ejecutá **Tools → Assets de Mazmorra → Colocar anillo y guardianes en mi circuito**.

La acción añade un grupo, retira la instancia central del guardián anterior si existe, separa las cuatro jardineras del borde de la cuenca y aplica piedra gris a paredes, techo, arcos y bordes. No reconstruye la geometría ni cambia las trampas o la cámara. Podés deshacer la acción con **Ctrl+Z**. Si el grupo nuevo ya existe, lo selecciona en lugar de duplicarlo.

Si preferís conservar toda tu decoración, arrastrá los prefabs individualmente desde la biblioteca.

## Las nueve piezas nuevas

| Prefab | Uso |
|---|---|
| Anillo_Lava_Cadenas | Conjunto central completo, cuenca y cuatro cadenas al techo |
| Anillo_Suspendido_Lava | Anillo y diez cascadas con espacios entre ellas |
| Cuenca_Lava_Circular | Cuenca inferior de unos 97 m de diámetro |
| Cadena_Modulo_8m | Tramo de cadena para repetir, rotar o escalar |
| Eslabon_Cadena | Eslabón individual para remates |
| Guardian_Martillo_Gris | Guardián con martillo elevado |
| Guardian_Espada_Gris | Guardián con espada larga |
| Guardian_Escudo_Gris | Guardián con escudo amplio y lanza corta |
| Guardian_Llave_Gris | Guardián con llave monumental |

## Modificar el centro

Expandí **Anillo_Lava_Cadenas** en Hierarchy. Podés mover el anillo y la cuenca por separado. Las cascadas son diez superficies individuales: cada una admite cambios de posición, rotación y escala. Para variar la altura, modificá el anillo y las cascadas; ajustá la escala Y y la posición Y de cada cascada para conectar su parte superior con el anillo y su parte inferior con la cuenca.

Las cadenas son estáticas y no usan físicas. Cada **Cadena_1…4** agrupa sus eslabones: podés mover o escalar el grupo completo, o modificar eslabones. Los cuatro **Anclaje_Techo** se colocan bajo el techo de 60 m del circuito de ejemplo. Si bajás el techo o movés el anillo, también hay que ajustar las cadenas y sus anclajes; no se recalculan automáticamente. El prefab **Cadena_Modulo_8m** sirve para completar recorridos a mano.

La lava se mueve mediante el componente **Lava Flow Visual** de las piezas de anillo y cuenca. En Inspector, **Flow Speed** cambia la velocidad visual: 0 deja la textura quieta. Esta lava es decorativa; no frena, no daña y no tiene colisiones. Los guardianes y las cadenas tampoco añaden colisiones a la conducción.

Los modelos nuevos tienen piezas y mallas normales de Unity. Se modifican mediante sus objetos hijos y Transform. La carretera sigue siendo editable con ProBuilder.

## Visibilidad

Las posiciones se revisaron con capturas de la cámara Cinemachine del kart, sin cambiar su lente de 60°. Los cuatro guardianes sirven como referencias al aproximarse a sus esquinas. El centro y las cadenas aparecen cuando entran en el encuadre; el anillo no permanece visible durante toda la vuelta con una cámara que mira hacia adelante.

## Paquete independiente

**Anillo_Lava_Cadenas.unitypackage** incluye la biblioteca completa: 76 prefabs, 8 texturas y 3 escenas de catálogo/ejemplo decorativo. Requiere un proyecto URP. Los menús del circuito y la escena jugable están en el repositorio KartMicroGame, porque dependen de sus scripts y trampas; el paquete de decoración no incorpora el juego completo.

Las nuevas texturas de piedra y lava se generaron con ImageGen. Los modelos, las mallas, los prefabs y las capturas se generaron y comprobaron en Unity. Los prompts están en **Documentation/Prompts_Anillo_Lava.json**.
