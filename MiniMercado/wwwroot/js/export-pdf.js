/**
 * MiniMercado - Exportación a PDF
 * Utiliza jsPDF y jsPDF-AutoTable
 */

async function exportTableToPDF(tableId, titulo, columnas, opciones = {}) {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({
        orientation: opciones.orientation || 'portrait',
        unit: 'mm',
        format: 'a4'
    });

    const orientation = opciones.orientation || 'portrait';
    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();
    const margin = 14;

    // 1. Cargar Logo
    let logoBase64 = null;
    try {
        logoBase64 = await getLogoBase64('/images/Minimercado.png');
    } catch (e) {
        console.warn("No se pudo cargar el logo:", e);
    }

    // 2. Función para dibujar Header y Footer
    const addHeaderFooter = (data) => {
        // Header
        doc.setFontSize(10);
        doc.setTextColor(100);
        
        // Logo
        if (logoBase64) {
            doc.addImage(logoBase64, 'PNG', margin, 10, 15, 15);
        }

        // Nombre del Comercio y Fecha
        const comercioNombre = document.getElementById('comercio-nombre-hidden')?.value || "MiniMercado";
        doc.setFont("DM Sans", "bold");
        doc.text(comercioNombre, margin + (logoBase64 ? 20 : 0), 15);
        
        doc.setFont("DM Sans", "normal");
        const fechaStr = new Date().toLocaleString();
        doc.text(`Generado: ${fechaStr}`, pageWidth - margin, 15, { align: 'right' });

        // Título del Reporte
        doc.setFontSize(18);
        doc.setTextColor(79, 70, 229); // #4f46e5 (Indigo)
        doc.setFont("DM Sans", "bold");
        doc.text(titulo, pageWidth / 2, 30, { align: 'center' });

        // Footer
        const str = "Página " + doc.internal.getNumberOfPages();
        doc.setFontSize(9);
        doc.setTextColor(150);
        doc.setFont("DM Sans", "normal");
        doc.text(str, pageWidth / 2, pageHeight - 10, { align: 'center' });
        doc.text("MiniMercado - Sistema de Gestión", margin, pageHeight - 10);
    };

    // 3. Configurar AutoTable
    const tableElement = document.getElementById(tableId);
    if (!tableElement) {
        console.error("Tabla no encontrada:", tableId);
        return;
    }

    const excluirIndices = opciones.excluirColumnas || [];
    
    doc.autoTable({
        html: `#${tableId}`,
        startY: 40,
        margin: { top: 40, bottom: 20 },
        headStyles: {
            fillColor: [79, 70, 229], // #4f46e5
            textColor: 255,
            fontSize: 10,
            fontStyle: 'bold',
            halign: 'left'
        },
        bodyStyles: {
            fontSize: 9,
            textColor: 50
        },
        alternateRowStyles: {
            fillColor: [248, 250, 252] // #f8fafc
        },
        didDrawPage: addHeaderFooter,
        didParseCell: function(data) {
            // Excluir columnas de acciones
            if (excluirIndices.includes(data.column.index)) {
                data.cell.text = ''; 
                // Nota: autoTable no elimina la columna fácilmente por índice aquí si viene del HTML, 
                // pero podemos limpiar el contenido o usar 'columns' param si fuera más complejo.
            }
            
            // Limpiar iconos o botones que puedan venir en el texto
            if (data.section === 'body') {
                // Eliminar espacios en blanco excesivos y caracteres extraños de los botones
                data.cell.text = data.cell.text.toString().trim().replace(/\s\s+/g, ' ');
            }
        },
        // Filtrar columnas antes de renderizar
        columns: Array.from(tableElement.querySelectorAll('thead th'))
            .map((th, index) => ({ header: th.innerText.trim(), dataKey: index }))
            .filter((col, index) => !excluirIndices.includes(index))
    });

    // 4. Guardar y Opcionalmente Imprimir
    const fileName = opciones.fileName || `${titulo.replace(/\s+/g, '_')}_${new Date().getTime()}.pdf`;
    
    // Abrir diálogo de impresión directamente (en una nueva pestaña)
    const pdfBlob = doc.output('bloburl');
    const printWindow = window.open(pdfBlob, '_blank');
    if (printWindow) {
        printWindow.onload = () => {
            printWindow.print();
        };
    } else {
        // Fallback a descarga si el popup está bloqueado
        doc.save(fileName);
    }
}

/**
 * Convierte una imagen a Base64 usando Canvas
 */
function getLogoBase64(url) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = 'Anonymous';
        img.onload = () => {
            const canvas = document.createElement('canvas');
            canvas.width = img.width;
            canvas.height = img.height;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0);
            const dataURL = canvas.toDataURL('image/png');
            resolve(dataURL);
        };
        img.onerror = reject;
        img.src = url;
    });
}
