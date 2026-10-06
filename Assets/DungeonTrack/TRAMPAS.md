# Aceleradores y pinches por vuelta

## Abrir el ejemplo

Abrí `Assets/DungeonTrack/DungeonCircuit_Trampas.unity` y presioná Play. La escena conserva la pista de ProBuilder y agrega tres zonas en la curva de la sección 2.

En Hierarchy, desplegá `TRAMPAS_POR_VUELTA` y seleccioná `Zona_A_Acelerador_Pinches`, `Zona_B_Acelerador_Pinches` o `Zona_C_Acelerador_Pinches`.

En su Inspector, **Estado en cada vuelta** muestra **Vuelta 1**, **Vuelta 2** y **Vuelta 3**. Elegí para cada una:

- **Acelerador:** boost temporal de velocidad máxima y aceleración.
- **Pinches:** frenado progresivo hasta detener la marcha del kart; recupera la aceleración al detenerse.
- **Inactiva:** sin efecto, con placa gris.

La configuración del ejemplo es:

| Vuelta | Zona A | Zona B | Zona C |
| --- | --- | --- | --- |
| 1 | Acelerador | Acelerador | Acelerador |
| 2 | Acelerador | Pinches | Acelerador |
| 3 | Pinches | Pinches | Pinches |

El estado cambia al **comenzar** una vuelta nueva. El primer cruce de la meta inicia la primera vuelta y conserva su configuración. Terminar la tercera vuelta no activa una cuarta.

## Agregar las zonas a tu pista editada

Si ya modificaste la escena anterior, abrila y ejecutá **Tools → Dungeon Track → Agregar aceleradores y pinches a la escena actual**. Agrega el grupo y las tres zonas sobre los tramos existentes de la curva Z2. Guarda con Ctrl+S. La operación admite Undo y no regenera la pista. Si ya hay un controlador, el menú lo selecciona sin agregar otro.

Para agregar otra zona, arrastrá el prefab `Assets/DungeonTrack/Prefabs/Zona_Acelerador_Pinches.prefab` a la escena. Ajustá su posición y rotación; su eje azul Z apunta en el sentido de circulación. Escalá su Transform en X y Z para cambiar el ancho y largo del rectángulo junto con su apariencia. Después agregala a la lista **Traps** del componente **Lap Trap Controller**, en `TRAMPAS_POR_VUELTA`.

El controlador debe referenciar el **ObjectiveCompleteLaps** activo de la carrera. El objetivo sigue contando vueltas y mostrando el HUD original; las trampas escuchan su evento de nueva vuelta. En ausencia de una referencia, el controlador busca el objetivo en su propia escena y advierte si no lo encuentra.

## Ajustar los efectos

- **Brake Deceleration:** frenado adicional en m/s²; valor inicial 12. Un valor mayor detiene más rápido. El efecto sigue hasta detener el kart aunque este salga del rectángulo. Conserva el movimiento normal de caída/suspensión y frena la marcha sobre la carretera.
- **Boost Stats → Max Time:** duración del boost, inicialmente 3 segundos.
- **Boost Stats → Modifiers → Top Speed:** aumento de velocidad máxima, inicialmente +5 m/s.
- **Boost Stats → Modifiers → Acceleration:** aumento de aceleración, inicialmente +5.

El acelerador usa `ArcadeKart.AddPowerup`, el mismo sistema del `SpeedPad` del proyecto. Cada activación tiene su propio temporizador. Volver a entrar en la misma zona renueva su boost sin acumularlo indefinidamente; los boosts de otras zonas conservan sus efectos.

Los pinches deshabilitan temporalmente aceleración, marcha atrás y derrape; se puede dirigir el kart durante el frenado. Al llegar a cero, se recupera el control normal en el siguiente paso de física. Este bloqueo es independiente de los de la cuenta regresiva y los rebotes contra paredes.

Cada kart activa una zona una vez por pasada, aunque tenga varios colliders. Puede detenerse y acelerar dentro de los pinches sin quedar atrapado. Se habilita otra activación cuando todos sus colliders salen y vuelve a entrar. Si cambia la vuelta mientras está encima, la apariencia se actualiza y el nuevo efecto se aplica en la próxima entrada.

El mismo BoxCollider permanece como **Is Trigger** en los tres estados. Los pinches y las flechas son señales visuales sin colliders sólidos; el piso y los bordes de la pista conservan sus colisiones.

## Próximas trampas

Los scripts de juego están en `Assets/Karting/Scripts/Traps` dentro de la misma assembly `KartGame` que el kart y el objetivo.

Una trampa nueva (rocas, piso que cae, hacha, etc.) puede heredar de `LapTrap`, implementar `ApplyLap(int lapNumber)` y agregarse a la lista del controlador. El número recibido comienza en 1. Cada tipo decide qué parámetros aplicar por vuelta: cantidad de rocas, tiempo de aviso, activación, velocidad, etc. También se puede escuchar `LapTrapController.LapChanged` sin modificar el contador.

Las zonas admiten más de tres vueltas mediante **Cantidad de vueltas configuradas**. Si la carrera supera las entradas configuradas, usan la última; sin entradas, quedan inactivas. La configuración es manual en el Inspector.

## Pruebas

Las pruebas de física y vueltas están en `Assets/Tests/PlayMode/LapTrapTests.cs`. Se ejecutan en **Window → General → Test Runner → PlayMode**. Verifican integración con el objetivo original, frenado con acelerador sostenido, recuperación, colliders múltiples, reentrada, temporizadores independientes y respeto del bloqueo de movimiento existente.

Las zonas del ejemplo son una disposición inicial editable. Su tamaño, duración y frenado se pueden ajustar después de conducir la pista completa.
