# Rocas con recorridos editables

## Probar el ejemplo

Después de Assets → Refresh (Ctrl+R), abrí **Assets/DungeonTrack/DungeonCircuit_Rocas.unity** y presioná Play. Conserva la pista y los tres aceleradores/pinches; agrega tres recorridos en la pendiente violeta, de la izquierda alta a la derecha baja, hacia el kart. Las rocas comienzan después de la cuenta regresiva y una espera inicial de 1 segundo.

En Hierarchy, abrí **TRAMPAS_POR_VUELTA → ROCAS_POR_VUELTA**. Seleccioná **ROCAS_POR_VUELTA** para configurar las emisiones y desplegá **Recorrido_A**, **Recorrido_B** o **Recorrido_C** para mover sus puntos.

## Configuración en el Inspector

En el componente **Rock Trap**, cada vuelta tiene:

| Campo | Qué cambia |
| --- | --- |
| Generar rocas | Activa o desactiva las nuevas caídas en esa vuelta. |
| Velocidad al rodar (m/s) | Velocidad constante después del impacto. |
| Tiempo entre caídas (segundos) | Intervalo mínimo entre apariciones, independientemente del recorrido elegido. |
| Detención del kart (segundos) | Tiempo que el kart queda detenido al tocar una roca. Cero = sin detención ni daño visual. |
| Recorridos habilitados | Arrastrá los recorridos que participan. Vacío significa todos; para ninguno, desactivá Generar rocas. |

Valores iniciales:

| Vuelta | Velocidad | Intervalo | Detención |
| --- | --- | --- | --- |
| 1 | 6 m/s | 4 s | 1 s |
| 2 | 9 m/s | 3 s | 1,5 s |
| 3 | 12 m/s | 2 s | 2 s |

**Cantidad de vueltas configuradas** permite agregar más entradas. Si la carrera supera las configuradas, utiliza la última; sin entradas no emite rocas. El controlador de vueltas existente aplica estos cambios: no hay un segundo contador.

Las rocas que ya están en la pista terminan con los valores que tenían al aparecer. Cambiar de vuelta o desactivar las emisiones configura las siguientes rocas. Al terminar la carrera se retiran las restantes.

**Orden de caída**:

- **Aleatorio por tandas:** mezcla los recorridos habilitados, usa cada uno una vez y mezcla de nuevo. Evita repetir el mismo recorrido al cambiar de tanda si hay más de uno.
- **Secuencia editable:** arrastrá recorridos a la lista Secuencia y reordená sus filas. Admite repeticiones, por ejemplo B → A → B → C. Los recorridos deshabilitados en esa vuelta se saltan. Una secuencia vacía usa el orden de Recorridos.

No se generan tres rocas simultáneamente. Pueden quedar varias rodando. **Máximo de rocas en pista** limita cuántas permanecen activas; si se alcanza, espera a que haya lugar. **Espera inicial** controla la primera aparición después del inicio de carrera. **Esperar inicio de carrera** y la referencia a su **TimeManager** evitan emisiones durante la cuenta regresiva. Desactivá esa opción solo para probar recorridos sin una carrera.

## Mover los puntos

Seleccioná el objeto **Recorrido_A/B/C**. El componente **Rock Route** muestra:

- **Aparición:** objeto vacío donde aparece el centro de la roca.
- **Impacto:** centro de la roca al terminar de caer y empezar a rodar.
- **Puntos al rodar:** lista ordenada; el último es la desaparición. Podés arrastrar filas para reordenarlos.
- **Diámetro de roca:** tamaño en metros.
- **Duración de caída:** tiempo desde Aparición hasta Impacto; la caída acelera visualmente.
- **Movimiento entre puntos:** recto o suavizado. El suavizado pasa por los puntos, pero puede separarse del suelo entre ellos; agregá puntos o usá Recto para seguir pendientes con precisión.

Mové los objetos vacíos con Move/W. Al seleccionar el recorrido aparecen sus líneas, etiquetas y controles en Scene; los cambios admiten Undo. **Crear un punto al final** crea y agrega un objeto vacío. **Colocar Aparición sobre Impacto** permite elegir la altura de la caída.

Los puntos representan **el centro de la roca**, no el suelo. Dejá aproximadamente medio diámetro sobre la carretera. Si cambiás el diámetro o editás la altura del suelo con ProBuilder, reajustá los puntos. Cada roca copia el recorrido al aparecer: los cambios durante Play se ven en las siguientes.

Las rocas se desplazan por estos puntos y giran solo para dar apariencia de rodado. El Rigidbody es cinemático, sin gravedad, y su SphereCollider es un trigger. No rebotan, no se desvían al tocar paredes y no empujan físicamente al kart. Mantené los recorridos dentro de la pista; los controles sirven para que puedas dirigirlos deliberadamente.

## Golpe y apariencia del kart

El contacto detiene inmediatamente la velocidad del kart y bloquea aceleración, marcha atrás y giro por los segundos configurados. Queda detenido incluso sobre la pendiente. Al terminar, restaura sus restricciones originales y recupera los controles, respetando la cuenta regresiva y los rebotes existentes.

Cada roca golpea una sola vez a cada kart, aunque haya varios colliders o siga tocándolo. Otra roca puede renovar la detención: se conserva el mayor tiempo restante, no se suman las duraciones. La roca continúa su recorrido después del golpe.

**Intensidad de oscurecimiento** controla cuánto se oscurece el kart. **Parpadeos por segundo** controla el pulso; cero deja un oscurecimiento fijo. Al recuperarse, vuelve a su aspecto original. El efecto no cambia los materiales compartidos.

## Instalar sobre la escena que ya editaste

Abrí tu escena y ejecutá **Tools → Dungeon Track → Agregar rocas a la escena actual**. Usa los vértices y las alturas actuales de los primeros tres tramos **Z4_Curva_Amplia**, agrega recorridos y registra la trampa en el controlador existente. No regenera la pista; admite Undo y no duplica las rocas si ya existe el grupo. Guardá con Ctrl+S.

Si cambiaste la topología de esos tramos, podés configurar los recorridos manualmente: creá un objeto vacío, agregá **Rock Route**, asigná Aparición, Impacto y los puntos finales. Arrastralo a **Recorridos** del controlador; agregalo también a los recorridos habilitados de las vueltas que deban usarlo. Si usás Secuencia, incluilo allí.

El prefab es **Assets/DungeonTrack/Prefabs/Roca_Redonda.prefab**. Para otra apariencia, duplicalo, conservá **Rock Hazard**, su trigger y Rigidbody cinemático, cambiá el hijo visual y asigná el nuevo prefab al controlador. El hijo visual debe tener diámetro local 1; el recorrido aplica el tamaño final.

Las pruebas están en **Assets/Tests/PlayMode/RockTrapTests.cs** y **Assets/Tests/Editor/DungeonRockSceneTests.cs**, junto a las pruebas anteriores de las zonas. Se verifican movimiento, desaparición, secuencia, vueltas, meta y kart reales, contactos rápidos, recuperación, daño visual, conservación de la pista e instalación con Undo.

Los valores son una configuración inicial. Falta conducir la vuelta completa para ajustar dificultad, frecuencia y tamaños.
