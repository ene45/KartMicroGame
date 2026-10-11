# Paquete Medieval Cartoon

67 prefabs 3D, 6 texturas originales y materiales para Unity 6 con URP. Piedra cálida, madera, hierro oscuro, estandartes bordó y vegetación verde. Las piezas son modelos reales con sus mallas guardadas en el proyecto.

## Encontrar y colocar los assets

1. En la ventana Project, abrí **Assets → MedievalCartoon → Prefabs**.
2. Elegí una categoría y arrastrá el prefab a la escena o a Hierarchy.
3. Ajustá Position, Rotation y Scale desde Inspector. Un metro equivale a una unidad de Unity.
4. Expandí el objeto para editar sus partes, colores, materiales, llamas y luces por separado. Para cambios propios, creá una Prefab Variant o editá una copia del prefab.

Abrí **Assets/MedievalCartoon/Scenes/Catalogo_MedievalCartoon.unity**, o usá **Tools → Dungeon Track → Medieval Cartoon → Abrir catalogo**, para ver todos los modelos juntos. La galería muestra algunos modelos grandes reducidos para que entren en los expositores: el prefab original conserva sus dimensiones.

**Ejemplo_Guia_Sin_Flechas.unity** muestra una bifurcación orientada mediante pavimento, antorchas, bordes y un portal abierto. El otro acceso está cerrado con reja y objetos.

En este proyecto también se incluye **Assets/DungeonTrack/DungeonCircuit_MedievalCartoon.unity**: una copia de la escena con rocas que incorpora materiales, landmark y decoración. Las escenas anteriores conservan sus archivos y su pista ProBuilder. El paquete independiente contiene la galería y el ejemplo de orientación; la pista jugable requiere el resto de Kart Microgame y está en el repositorio.

## Contenido

| Categoría | Prefabs | Uso |
|---|---:|---|
| Arquitectura | 20 | Paredes de 4/8 m, esquina, ruina, columnas, arcos de 4/8/18 m, arcada doble, puerta, reja, baldosas, bóveda, viga, escalera y contrafuerte |
| Iluminación | 9 | Antorcha suelta, de pared, doble, braseros bajo/alto, trípode, de suelo, colgante y poste |
| Decoración | 13 | Barriles, cajas, cofres, estandartes, escudo, martillo y escombros |
| Naturaleza | 9 | Macetas, tres rocas, grupo de rocas, arbusto, ciprés y matas |
| Guía sin flechas | 13 | Bordes, valla, mojones, paso iluminado, pavimento claro, desgaste de ruedas, repeticiones, portal, bloqueo y jardinera |
| Landmark | 3 | Guardián completo, estatua y pedestal separados |

Las medidas individuales están en **INVENTARIO.md**. El guardián completo ocupa aproximadamente **20 × 20 m** y mide **24,7 m** de alto. Su origen está al pie del pedestal y su frente mira hacia **+Z**.

En la pista de ejemplo está colocado con escala 1,8 para ocupar mejor el espacio central: unos 36 m de ancho y 44,5 m de alto. El prefab original conserva su escala base.

## Orientar al jugador sin flechas

- **Continuidad:** repetí mojones o antorchas al costado de la ruta. En una curva, la sucesión debería permitir ver hacia dónde continúa el borde antes de entrar.
- **Contraste de piso:** usá pavimento claro para destacar un recorrido. Evitá cambiar el significado de ese material en otras zonas.
- **Enmarcar:** colocá arcos abiertos y dos braseros en un acceso importante. Los huecos deben mostrar la continuación de la pista.
- **Cerrar accesos:** usá Reja_Cerrada, Puerta_Cerrada o Bloqueo_Cajas_Barriles en entradas que no son transitables. Colocarlos sobre una ruta transitada sí bloqueará al kart.
- **Referencias:** el guardián permite reconocer la zona y la orientación general. Evitá que oculte salidas o próximas curvas.
- **Bordes:** las jardineras, vallas y bordes bajos delimitan el espacio. Colocalos fuera del ancho útil de pista, especialmente en curvas, saltos y rutas de rocas.

Las antorchas pueden enmarcar las tres rutas de la entrada; no hace falta sugerir una única opción cuando los tres caminos son válidos. Estas piezas aportan señales visuales: la claridad final del recorrido se debe comprobar conduciendo a la velocidad real del kart.

## Ajustar antorchas

Expandí el prefab y seleccioná el hijo **Fuego**. Su componente **Cartoon Torch** permite modificar:

- **Light Enabled:** encender o apagar la luz sin quitar la llama visible.
- **Intensity, Range, Color:** potencia, alcance y tono.
- **Flicker y Flicker Speed:** cantidad y velocidad de parpadeo.

Las luces no proyectan sombras por defecto. Para muchas antorchas, mantené sus alcances pequeños y dejá la luz activada solamente en las más importantes; la llama puede seguir visible. El parpadeo funciona durante Play Mode. No hay partículas ni físicas en las llamas.

## Materiales, colliders y edición

Las texturas se importan con Repeat. En un material podés cambiar **Base Map → Tiling** para adaptar el tamaño del patrón a otro objeto. Incluyen pared, techo, adoquines, pasto, madera y piedra continua para escultura; son texturas de color, sin mapas normales adicionales.

Las partes sólidas tienen colliders estáticos. Los arcos abiertos respetan el hueco; la reja cerrada lo bloquea. La vegetación pequeña, telas y llamas no tienen colliders. Podés desactivar colliders de piezas puramente decorativas desde Inspector.

La pista mantiene su edición ProBuilder. Los nuevos props se editan mediante su jerarquía de partes y mallas nativas; no dependen de ProBuilder para funcionar. Los modelos no tienen rig de personajes.

**Tools → Dungeon Track → Medieval Cartoon → Agregar landmark a escena** agrega un guardián en el origen y permite posicionarlo. **Aplicar tema a escena actual** agrega el tema a una escena del circuito de mazmorra; se puede deshacer con Undo. Si ya existe el grupo de decoración, no vuelve a duplicarlo. No se regenera nada al ejecutar Refresh.

## Importar en otro proyecto

Importá **MedievalCartoon_Assets.unitypackage** mediante **Assets → Import Package → Custom Package**. Requiere Unity 6 con URP; incluye mallas, materiales, prefabs, texturas, script de antorchas, catálogo y ejemplo. No incluye el juego ni los scripts de trampas.

## Procedencia

Las mallas se crearon para este paquete. Las seis texturas se generaron con la herramienta integrada ImageGen usando como referencia de estilo la opción Medieval Cartoon elegida. **Prompts_Texturas.json** conserva el modo y los prompts completos. No se descargaron modelos ni texturas de terceros.
