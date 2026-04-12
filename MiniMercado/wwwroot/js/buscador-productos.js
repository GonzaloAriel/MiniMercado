/**
 * Componente Reutilizable de Búsqueda de Productos
 * @param {Object} config Configuración del buscador
 */
function initBuscador(config) {
    const input = document.getElementById(config.searchId);
    const hidden = document.getElementById(config.inputId);
    const resultDiv = document.getElementById(config.resultId);
    const nameInput = config.nameInputId ? document.getElementById(config.nameInputId) : null;
    let selectedIndex = -1;
    let timeout = null;
    let abortController = null; // Para cancelar peticiones previas
    const cache = {}; // Caché local en memoria

    input.addEventListener("input", function () {
        clearTimeout(timeout);
        let query = this.value.trim();

        if (query.length < 2) {
            ocultarResultados();
            return;
        }

        // Si ya tenemos el resultado en caché, lo mostramos de inmediato
        if (cache[query]) {
            renderizarResultados(cache[query]);
            return;
        }

        timeout = setTimeout(() => {
            // Cancelamos la petición anterior si existe
            if (abortController) {
                abortController.abort();
            }
            abortController = new AbortController();

            let url = `/api/productosapi/buscar?term=${encodeURIComponent(query)}`;
            if (config.params) {
                for (const key in config.params) {
                    url += `&${key}=${config.params[key]}`;
                }
            }

            fetch(url, { signal: abortController.signal })
                .then(res => res.json())
                .then(data => {
                    cache[query] = data; // Guardamos en caché
                    renderizarResultados(data);
                })
                .catch(err => {
                    if (err.name !== 'AbortError') {
                        console.error("Error en búsqueda:", err);
                    }
                });
        }, 300);
    });

    input.addEventListener("keydown", function (e) {
        const items = resultDiv.querySelectorAll(".list-group-item");
        
        if (e.key === "ArrowDown") {
            e.preventDefault();
            if (items.length > 0) {
                selectedIndex = (selectedIndex + 1) % items.length;
                actualizarSeleccion(items);
            }
        } else if (e.key === "ArrowUp") {
            e.preventDefault();
            if (items.length > 0) {
                selectedIndex = (selectedIndex - 1 + items.length) % items.length;
                actualizarSeleccion(items);
            }
        } else if (e.key === "Enter") {
            e.preventDefault();
            if (selectedIndex > -1 && items[selectedIndex]) {
                const p = JSON.parse(items[selectedIndex].getAttribute("data-producto"));
                ejecutarSeleccion(p);
            } else if (items.length === 1) {
                const p = JSON.parse(items[0].getAttribute("data-producto"));
                ejecutarSeleccion(p);
            }
        } else if (e.key === "Escape") {
            ocultarResultados();
        }
    });

    function renderizarResultados(data) {
        resultDiv.innerHTML = "";
        if (data.length === 0) {
            ocultarResultados();
            return;
        }

        data.forEach((p, index) => {
            const item = document.createElement("a");
            item.href = "#";
            item.className = "list-group-item list-group-item-action d-flex justify-content-between align-items-center py-2";
            item.setAttribute("data-producto", JSON.stringify(p));
            
            const isBase = p.tipo === "base";
            const badgeClass = isBase ? "bg-warning text-dark" : "bg-light text-dark border";
            const stockText = isBase ? "📦 Catálogo Global" : `Stock: ${p.stock}`;

            item.innerHTML = `
                <div>
                    <strong>${isBase ? "✨ " : ""}${p.nombre}</strong>
                    <br><small class="text-muted">${p.codigo}</small>
                </div>
                <div class="text-end">
                    <span class="fw-bold text-success">$${p.precio}</span>
                    <br><small class="badge ${badgeClass}">${stockText}</small>
                </div>
            `;

            item.onclick = (e) => {
                e.preventDefault();
                ejecutarSeleccion(p);
            };

            resultDiv.appendChild(item);
        });

        resultDiv.classList.remove("d-none");
        selectedIndex = -1;
    }

    function ejecutarSeleccion(p) {
        if (p.tipo === "base") {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            fetch('/api/productosapi/crear-desde-base', {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ baseId: parseInt(p.id) })
            })
            .then(res => {
                if (!res.ok) throw new Error("Error en servidor");
                return res.json();
            })
            .then(nuevoProducto => {
                nuevoProducto.esBase = true;
                finalizar(nuevoProducto);
            })
            .catch(err => {
                console.error("Error al crear producto:", err);
                alert("No se pudo vincular el producto.");
            });
        } else {
            finalizar(p);
        }
    }

    function finalizar(p) {
        // Limpiamos todo
        clearTimeout(timeout);
        if (abortController) abortController.abort();
        
        hidden.value = p.id;
        if (nameInput) {
            nameInput.value = p.nombre;
        }
        
        input.value = ""; 
        
        // Un delay mínimo para asegurar que nada re-llene el input
        setTimeout(() => {
            input.value = "";
            ocultarResultados();
            input.focus();
        }, 50);

        if (config.onSelect) {
            config.onSelect(p);
        }
    }

    function actualizarSeleccion(items) {
        items.forEach((item, i) => {
            if (i === selectedIndex) {
                item.classList.add("active");
                item.scrollIntoView({ block: "nearest" });
            } else {
                item.classList.remove("active");
            }
        });
    }

    function ocultarResultados() {
        resultDiv.classList.add("d-none");
        resultDiv.innerHTML = "";
        selectedIndex = -1;
    }

    document.addEventListener("click", function (e) {
        if (!input.contains(e.target) && !resultDiv.contains(e.target)) {
            ocultarResultados();
        }
    });
}
