/**
 * Componente Reutilizable de Búsqueda de Productos
 * @param {Object} config Configuración del buscador
 */
function initBuscador(config) {
    const input = document.getElementById(config.searchId);
    const hidden = document.getElementById(config.inputId);
    const resultDiv = document.getElementById(config.resultId);
    let selectedIndex = -1;
    let timeout = null;

    input.addEventListener("input", function () {
        clearTimeout(timeout);
        let query = this.value.trim();

        if (query.length < 2) {
            ocultarResultados();
            return;
        }

        timeout = setTimeout(() => {
            fetch(`/api/productosapi/buscar?term=${encodeURIComponent(query)}`)
                .then(res => res.json())
                .then(data => {
                    renderizarResultados(data);
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
                // Seleccionamos el producto asociado al item activo
                const p = JSON.parse(items[selectedIndex].getAttribute("data-producto"));
                ejecutarSeleccion(p);
            } else if (items.length === 1) {
                // Si hay uno solo, lo seleccionamos directamente
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
            console.log("Iniciando creación de producto base:", p.id);
            
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
                if (!res.ok) {
                    return res.text().then(text => {
                        console.error("ERROR SERVER:", text);
                        alert("ERROR REAL DEL SERVIDOR:\n" + text);
                        throw new Error(text);
                    });
                }
                return res.json();
            })
            .then(nuevoProducto => {
                console.log("Producto vinculado con éxito:", nuevoProducto);
                finalizar(nuevoProducto);
            })
            .catch(err => {
                console.error("Error al crear producto:", err);
                alert("No se pudo vincular el producto del catálogo global. Verifique la consola para más detalles.");
            });
        } else {
            finalizar(p);
        }
    }

    function finalizar(p) {
        hidden.value = p.id;
        input.value = p.nombre;
        ocultarResultados();
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
