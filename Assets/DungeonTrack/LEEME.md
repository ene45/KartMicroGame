# Circuito de mazmorra

Esta entrega incluye la escena generada, sus materiales, vistas previas y el generador de editor. Importá el paquete en el proyecto Kart Microgame y abrí `Assets/DungeonTrack/DungeonCircuit 4.unity`.

Escena generada: `Assets/DungeonTrack/DungeonCircuit.unity` (o una copia con nombre nuevo si ya existe).

Abrí la escena en Unity 6000.0.42f1 y usá Play para probarla con el kart, la cámara, la interfaz y el sistema de vueltas del proyecto. La escena se construye a partir de una copia de MainScene; MainScene y los scripts de conducción originales se conservan.

## Recorrido

Se conduce en sentido horario, siguiendo el croquis. La meta está en la recta superior izquierda.

- Zona 1: meta, salto pequeño, arco de entrada, tres caminos y reunión antes de la zona 2. El central es corto; los laterales hacen un rodeo y dejan espacio para reservar la futura trampa del centro.
- Zona 2: curva amplia con peralte gradual hasta 9 grados.
- Zona 3: recta amplia para las futuras plataformas y segundo salto con recepción separada.
- Zona 4: curva inferior, subida hasta 8 m y bajada hacia el arco de salida.
- Exterior: vuelta por el tramo superior izquierdo y pasto, para desarrollar los futuros atajos.

La carretera principal tiene 18 m de ancho. Los tres caminos abren progresivamente desde bocas de 6 m hasta un ancho útil cercano a 18 m; las bocas juntas ocupan los 18 m del tramo común. No se implementaron trampas, boost, caída de plataformas ni decoración del landmark.

## Editar con ProBuilder

En la jerarquía, abrí `CIRCUITO_MAZMORRA_PROBUILDER`.

- `01_Pista_Editable_18m`: seleccionar un tramo y editar vértices, aristas o caras con ProBuilder. Hay siete bandas a lo ancho y divisiones aproximadamente cada 2 m a lo largo.
- `02_Paredes_Simples`: piezas independientes de las carreteras.
- `03_Techos_Ocultar_Para_Editar`: desactivar el grupo mientras editás y volver a activarlo para probar el interior.
- `04_Pasto_Exterior`: superficies de pasto separadas.
- `05_Arcos_Entrada_Salida`: arcos editables.
- `07_Reservas_Trampas_Y_Landmark`: referencias vacías para el trabajo posterior.

Cada tramo es una malla estática de ProBuilder con collider. No depende de un generador en ejecución. Las uniones de tramos tienen vértices coincidentes, pero pertenecen a objetos distintos: si movés un extremo, mové también el extremo del tramo vecino para mantener la unión. Los saltos tienen huecos intencionales.

El menú `Tools > Dungeon Track > Crear una copia nueva del circuito` genera otra escena con un nombre nuevo. No sobrescribe la escena que editaste.

## Validación y ajustes pendientes

`Validation.txt` registra la comprobación de los colliders y de la geometría. `Preview` contiene vistas de la pista; en planta y perspectiva los techos están ocultos para verla completa.

Los saltos tienen huecos iniciales de 3 m y 3,5 m. Sus medidas, aterrizajes y comportamiento a diferentes velocidades deben ajustarse conduciendo el kart, como se acordó. El bloque inicial es para iterar sobre la pista, no una calibración final de la carrera.

La nueva escena no reemplaza la escena del menú ni cambia la lista de escenas del build. Para probarla, abrila directamente. La integración con el menú se puede hacer al terminar de ajustar la pista.

El generador se compiló y ejecutó en Unity 6000.0.42f1 con ProBuilder 6.0.4. La escena contiene 125 mallas ProBuilder y 28 tramos de carretera. Pasaron 2715 comprobaciones de superficie mediante raycasts, la revisión de triángulos y uniones y la reapertura de la escena guardada con sus mallas y colliders conservados. Las vistas previas se capturaron desde esa escena en Unity.

Si tenés abierta una copia anterior llamada `DungeonCircuit.unity`, esa copia corresponde a la generación fallida inicial y conserva la pista original. Abrí la escena incluida indicada arriba. La nueva generación asigna identificadores independientes a los tutoriales para poder abrirla junto a MainScene sin colisiones.
