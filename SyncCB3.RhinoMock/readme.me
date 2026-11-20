# Análisis Técnico de la Especificación: Concrete Rhino REST API (v1)

> **Documento analizado:** `Concrete_Rhino_REST_API_Specification.pdf` (Fecha de revisión: 2026-10-20, Chris Kranich - Automation Logic, LLC).  
> **Proyecto:** `SyncCB3.RhinoMock` (`C:\Concretec\SyncCB3\SyncCB3.RhinoMock`)  
> **Destinatarios:** Equipo de desarrollo e integración de SyncCB3.

---

## 1. Resumen Ejecutivo y Propósito

El sistema **Concrete Rhino Automated Batching System** provee una interfaz REST API v1 diseñada para la integración bidireccional entre las plantas dosificadoras de concreto y los sistemas externos de Despacho (Dispatch), Control de Calidad (QC/Mix Design) y Administración/ERP.

### Principales Funcionalidades
* **Hacia la planta (Escritura):** Envío y actualización de clientes, recetas/diseños de mezcla, materiales (propiedades de humedad/absorción), órdenes de despacho y tickets de carga en cola de dosificación.
* **Desde la planta (Lectura):** Monitoreo de estado de planta, consulta de órdenes y cargas en proceso, y recolección detallada de resultados reales de dosificación (pesos teóricos vs. reales batcheados, humedades, aditivos y tiempos).

### Antecedentes y Relación con Estándares
* Basado en la especificación de la industria **BCQCI v3.4** (desarrollada con Stonemont Solutions, Noviembre 2017).
* **Mejoras y diferencias clave frente a BCQCI:**
  * Cada planta responde de manera individual (la URL no lleva código de planta).
  * Formato nativo en **JSON** estricto.
  * Añadida gestión completa de órdenes, cargas y tickets (concepto heredado del protocolo *Universal Link / ULink*).
  * Validación transaccional de tipo "todo o nada" (all-or-nothing).
  * Autenticación mediante claves de API (*API Keys*).
  * Polling eficiente de resultados mediante identificador incremental (`bt_resultid`).

---

## 2. Arquitectura de Red y Transporte

| Concepto | Especificación |
| :--- | :--- |
| **Topología** | **Instancia por planta (Per-Plant):** Cada computadora de planta ejecuta su propio servicio local. No hay gateway centralizado; las peticiones van directamente a la IP/Host de cada planta. |
| **Protocolo & Puerto** | HTTP (o HTTPS mediante túnel/Tailscale/Reverse Proxy) en el puerto **8080** (por defecto). |
| **URL Base** | `http://<plant_address>:8080/rhino_rest_api/v1/json/<recurso>/...` |
| **Formato** | JSON. Todas las peticiones con cuerpo y todas las respuestas son objetos JSON. |
| **Métodos HTTP** | <ul><li>`GET`: Consulta de registros.</li><li>`PUT`: Creación o actualización (Upsert / modificación parcial).</li><li>`POST`: Creación exclusiva (falla si el registro ya existe).</li><li>`DELETE`: Exclusivo para eliminar **tickets** que aún no hayan comenzado a dosificar.</li></ul> |

---

## 3. Seguridad y Autenticación

1. **Credenciales:**
   * Toda petición requiere una API Key generada localmente en la planta (*Settings > API Keys*).
   * Formato: Prefijo `rk_` seguido de **64 caracteres hexadecimales en minúsculas**.
   * Se almacena como hash en la base de datos de la planta; si se extravía, debe crearse una nueva.
2. **Encabezados HTTP:**
   * Preferido: `Authorization: Bearer rk_<token>`
   * Alternativo: `X-API-Key: rk_<token>`
3. **Roles / Permisos:**
   * **Read-only:** Solo admite peticiones `GET`. Cualquier `PUT`, `POST` o `DELETE` retorna `403 Forbidden`.
   * **Read-write:** Acceso completo.
4. **Protección contra ataques (Lockout):**
   * Tras 10 peticiones fallidas consecutivas (incluyendo peticiones sin clave) desde una misma dirección IP, la IP queda **bloqueada durante 1 minuto**. Peticiones correctas no reinician este contador durante el periodo.
5. **Cifrado en tránsito:**
   * En red local (LAN) se permite HTTP plano.
   * Para accesos remotos/WAN es mandatorio el uso de HTTPS o una red segura como Tailscale / VPN corporativa.

---

## 4. Particularidades Críticas del Protocolo ("Gotchas" de Implementación)

### ⚠️ Regla de Oro 1: El Código de Estado HTTP siempre es 200 OK
* A nivel de capa de transporte HTTP, el servidor web casi siempre responde con código `200 OK` (a menos que la URL no coincida con el patrón de la API, donde el servidor web emitirá un 404 HTML estándar).
* **El verdadero estado de la transacción viene en el cuerpo JSON dentro de `request_status`:**
  ```json
  {
    "request_status": {
      "status": 400,
      "msg": "BAD REQUEST",
      "detail": "Unknown mix_code."
    },
    "error_detail": "Order 445676: mix 3000XX not found."
  }
  ```
* **Consecuencia en C# / HttpClient:** No se debe confiar únicamente en `response.IsSuccessStatusCode`. Se debe deserializar el objeto `request_status` y verificar `request_status.status == 200` o `201`.

### ⚠️ Regla de Oro 2: Estructura de Wrappers y Arrays en el Payload
* Las peticiones de escritura (`PUT` / `POST`) exigen que el cuerpo esté envuelto en un arreglo con el nombre en plural del recurso, y cada elemento envuelto en el nombre en singular:
  ```json
  {
    "orders": [
      {
        "order": {
          "order_num": "445676",
          "mix_code": "3000INT",
          ...
        }
      }
    ]
  }
  ```
* Si la URL ya especifica un código (ej. `orders/445676`), el array debe contener **exactamente un solo elemento**. Si no contiene código en la URL, puede enviar múltiples elementos en lote.

### ⚠️ Regla de Oro 3: Modelo Transaccional "Todo o Nada"
* En peticiones batch de múltiples registros, la planta valida todos los registros antes de guardar. Si uno falla, **ningún registro se almacena** (rollback total) y `error_detail` señala el registro causante.
* **Excepción única:** El recurso `plants/` no es transaccional; escribe registro a registro y se detiene en el primer fallo.

### ⚠️ Regla de Oro 4: Actualizaciones Parciales (PUT)
* Salvo en `plants/`, un `PUT` solo reemplaza los campos provistos. Los campos omitidos conservan su valor actual en la base de datos de la planta.
* Para blanquear un campo de texto se envía `""`.
* Enviar `null` o una cadena vacía en un código clave (`cust_code`, `mix_code`, `order_num`) equivale a no enviarlo.

### ⚠️ Regla de Oro 5: Tipos de Datos y Tolerancia
* **Números:** Se aceptan como números o como strings (`5`, `5.0`, `"5"`). En respuestas, algunos campos numéricos pueden venir serializados como strings (ej. `"mat_dosagetype": "0"`). Los deserializadores en C# deben usar convertidores tolerantes (`JsonConverter`).
* **Fechas:** Utilizar estrictamente formato **`YYYY-MM-DD`**. El motor de la planta no distingue entre formato día-primero y mes-primero para días <= 12 (ej. `05-03-2026` se interpreta siempre como Mayo 3).
* **Horas:** Formato 24 horas `HH:MM` o `HH:MM:SS`. Se almacenan redondeadas al minuto.
* **Caracteres en URLs:** Códigos que contengan diagonales `/` (por ejemplo un material o receta llamada `3/4 Rock`) **no pueden ir en la URL** ni siquiera escapadas como `%2F`. Debe llamarse al endpoint base y pasar el código dentro del payload JSON. Códigos con `+` deben codificarse como `%2B`.

---

## 5. Catálogo de Recursos y Endpoints

### 5.1 Estado y Diagnóstico (Ping & Polling)
* `GET status/`: Retorna identidad de la planta, hora local, versión de API y software, y `newest_result_id`.
* `GET status/<result_id>`: Retorna además `results_pending` (cantidad de cargas finalizadas con ID superior al consultado). **Punto de partida ideal para polling.**

### 5.2 Plantas (`plants/`)
* `GET plants/`: Lista las plantas configuradas en la base local.
* `GET plants/<code>`: Retorna la planta propia de la computadora actual.
* `PUT / POST plants/`: Alta o modificación de plantas.
* *Nota:* En este recurso, los campos omitidos se blanquean en vez de preservarse.

### 5.3 Clientes (`customers/` y `customerlist/`)
* `GET customers/`: Lista completa de clientes (ordenados por tipo de crédito y código).
* `GET customers/<cust_code>`: Consulta un cliente individual.
* `GET customerlist/`: Versión compacta (`cust_code`, `cust_name`).
* `PUT / POST customers/`: Alta y actualización de clientes.
* *Gotcha `cust_udf2`:* En `GET` devuelve el descuento, pero en `PUT` define el nivel de precio (1–3). **Recomendación oficial del fabricante:** No enviar `cust_udf2` en peticiones PUT.

### 5.4 Materiales (`materials/` y `materiallist/`)
* `GET materials/`: Lista de materiales configurados en la planta.
* `GET materials/<mat_code>`: Detalle de un material.
* `GET materiallist/`: Versión compacta (`mat_code`, `mat_name`, `mat_type`).
* `PUT materials/` o `PUT materials/<mat_code>`: Actualización de propiedades de materiales (origen, gravedad específica `mat_spgr`, absorción `mat_absorption`, y humedad total `mat_totalmoisture`).
* **Restricción:** No se pueden crear nuevos materiales ni eliminarlos vía API; están ligados al cableado y tolvas físicas de la planta. `POST` o `DELETE` devuelven error.

### 5.5 Diseños de Mezcla / Recetas (`mixdesigns/` y `mixdesignlist/`)
* `GET mixdesigns/`: Todas las recetas con su información cabecera (`mixinfo`) y lista de componentes (`mixcomponents`).
* `GET mixdesigns/<mix_code>`: Una receta específica.
* `GET mixdesignlist/`: Lista compacta (`mix_code`, `mix_name`).
* `PUT / POST mixdesigns/`: Alta y actualización completa de recetas.
* **Reglas:**
  * Si se incluye `mixcomponents`, **reemplaza por completo** la fórmula anterior (los componentes omitidos son eliminados).
  * Máximo 24 componentes por mezcla.
  * Si la receta se está dosificando en ese instante, el cambio es rechazado con `409 Conflict`.

### 5.6 Órdenes de Despacho (`orders/`, `orderslist/`, `orderstatus/`, `orderloads/`)
* `GET orders/`: Órdenes abiertas (estados 1 al 5).
* `GET orders/<date>` o `orders/<from>/<to>`: Órdenes en rango de fechas (máximo 93 días).
* `GET orders/<order_num>`: Detalle de una orden.
* `PUT / POST orders/`: Creación y actualización de órdenes.
* `PUT orderstatus/`: Cambio rápido de estado de la orden (1: Send, 2: Will call, 3: Weather, 4: Loading, 5: On hold, 6: Complete, 7: Canceled).
* `GET orderloads/<order_num>`: Consulta una orden con todas sus cargas asociadas.
* **Reglas de negocio:**
  * Solo se puede cambiar la receta (`mix_code`) de una orden si la orden no tiene ninguna carga y su estado es 1, 2 o 3. Si ya tiene cargas, retorna `409 Order already has loads`.
  * La lectura de órdenes devuelve el nombre del cliente (`customer`), no su código (`cust_code`). Por ende, el JSON leído no puede ser reenviado tal cual en un PUT.

### 5.7 Tickets de Carga (`tickets/`)
* Representa una carga encolada y lista para dosificar en la pantalla del operador (estilo *Universal Link*).
* `PUT tickets/` o `PUT tickets/<ticket_num>`: Envía o actualiza un ticket.
* `DELETE tickets/<ticket_num>`: Elimina un ticket de la cola **siempre y cuando no haya iniciado la dosificación**.
* **Capacidades automáticas del ticket:**
  * En una sola petición puede crear o actualizar la orden (`order`), dar de alta el camión (`truck_num`) y el chofer (`driver_name`), asignar productos adicionales (`extras`) y totales de impresión (`totals`).
  * Si el ticket ya comenzó a dosificar en planta o fue cancelado por el operador, cualquier intento de modificación o eliminación retorna `409 Conflicts with what is stored`.

### 5.8 Cargas y Resultados de Dosificación (`loads/` y `batchresults/`)
* `GET loads/`: Cargas vistas desde la perspectiva de despacho (tiempos de entrega, chofer, camión, estado 1-6).
* `GET batchresults/`: Cargas con desglose de control de calidad (pesos objetivo `bc_target`, pesos reales `bc_actual`, humedad libre `bc_freemoisture`, aditivos y agua).
* `GET batchresults/since/<result_id>`: Retorna las cargas dosificadas posteriores a un ID de resultado específico (máximo 100 resultados por respuesta).

---

## 6. Flujo Recomendado de Sincronización para SyncCB3

Para sincronizar datos entre un sistema central y las plantas dosificadoras Concrete Rhino:

```
+-----------------------------------------------------------------------------------+
|                           FLUJO A: DESPACHO -> PLANTA                             |
+-----------------------------------------------------------------------------------+
  1. Clientes & Recetas:
     - Sincronizar clientes con PUT /customers/
     - Enviar/actualizar fórmulas con PUT /mixdesigns/
  2. Ajustes de Materiales:
     - Ajustar humedades de tolvas con PUT /materials/
  3. Programación de Entrega:
     - Crear la orden con PUT /orders/
     - Cuando el camión esté listo en planta, enviar PUT /tickets/<ticket_num>
       (El viaje aparece como "Ready" en la pantalla de la planta).
```

```
+-----------------------------------------------------------------------------------+
|                     FLUJO B: PLANTA -> DESPACHO (BATCH RESULTS)                   |
+-----------------------------------------------------------------------------------+
  1. El servicio en segundo plano almacena en BD el último `last_result_id` procesado.
  2. Cada X segundos consulta:
     GET /status/<last_result_id>
  3. Si `results_pending > 0`:
     Llamar a: GET /batchresults/since/<last_result_id>
     - Procesar los pesos reales y materias primas dosificadas.
     - Actualizar `last_result_id` con el valor del último registro procesado.
     - Si el campo `"more": 1`, solicitar de inmediato el siguiente lote.
  4. Conciliación diaria (1 vez al día):
     Llamar a `GET /batchresults/<YYYY-MM-DD>` para conciliar posibles cargas que
     fueron finalizadas de forma manual por el operador fuera de secuencia.
```

---

## 7. Lineamientos para la Implementación en `SyncCB3.RhinoMock`

Dado que el proyecto `SyncCB3.RhinoMock` está configurado como biblioteca .NET 10 con soporte de `Newtonsoft.Json`:

### 1. Modelos DTOs Fuertemente Tipados
* Diseñar DTOs con decoradores `[JsonProperty("...")]` respetando la nomenclatura en snake_case del API.
* Implementar un `JsonConverter` personalizado para números flotantes/enteros que acepte tanto números directos (`12.5`) como cadenas (`"12.5"`).

### 2. Wrapper Response & Manejo de Errores
* Implementar una clase genérica de respuesta:
  ```csharp
  public class RhinoApiResponse<T>
  {
      [JsonProperty("request_status")]
      public RhinoRequestStatus RequestStatus { get; set; } = new();

      [JsonProperty("error_detail")]
      public string? ErrorDetail { get; set; }

      public T? Data { get; set; }

      public bool IsSuccess => RequestStatus.Status is 200 or 201;
  }
  ```

### 3. Simulador Mock Server (`RhinoMockServer`)
* Permitir simular las respuestas de la planta para pruebas automatizadas (unit tests y UI tests en `SyncCB3.UITest`):
  * Simulación de autenticación (rechazo sin Bearer token o token inválido).
  * Simulación de bloqueo por 10 fallos de clave.
  * Base de datos en memoria para clientes, recetas, órdenes y cola de tickets.
  * Generación incremental de `bt_resultid` simulando la dosificación de un camión.
  * Soporte de códigos de error 400, 404 y 409 según las reglas de negocio descritas.

---

## 8. Conclusiones y Próximos Pasos

La API de Concrete Rhino es robusta, bien estructurada y altamente determinista, con un diseño transaccional seguro y mecanismos diseñados expresamente para evitar colisiones entre el operador en cabina y los sistemas automáticos de despacho.

**Acciones recomendadas en el código:**
1. Crear el cliente HTTP `RhinoApiClient` con soporte de autenticación Bearer y manejo transparente del envoltorio `request_status`.
2. Crear los DTOs de requests y responses (`RhinoPlant`, `RhinoCustomer`, `RhinoMaterial`, `RhinoMixDesign`, `RhinoOrder`, `RhinoTicket`, `RhinoBatchResult`).
3. Implementar el motor de Mock (`RhinoMockHandler` / `RhinoMockServer`) dentro de `SyncCB3.RhinoMock` para validar los casos de prueba de integración sin depender de una planta física.
