-- Filtro de pandoc para el DOCX del entregable. Traduce al formato Word lo que en el
-- PDF resuelve el CSS:
--   * cada <section class="pagina"> empieza en una pagina nueva;
--   * la seccion "horizontal" (diagrama E-R) va en una pagina apaisada;
--   * las capturas toman un ancho fijo segun su clase.

local salto = pandoc.RawBlock('openxml', '<w:p><w:r><w:br w:type="page"/></w:r></w:p>')

-- Fin de seccion de Word con pagina A4 (twips; 1 cm = 567). Un fin de seccion ya
-- inicia pagina nueva, asi que nunca se combina con un salto de pagina.
local function finSeccion(apaisada)
  local ancho, alto, orient = 11906, 16838, ''
  if apaisada then
    ancho, alto, orient = 16838, 11906, ' w:orient="landscape"'
  end
  return pandoc.RawBlock('openxml', string.format(
    '<w:p><w:pPr><w:sectPr><w:pgSz w:w="%d" w:h="%d"%s/>'
      .. '<w:pgMar w:top="1020" w:right="907" w:bottom="1020" w:left="907"'
      .. ' w:header="567" w:footer="567" w:gutter="0"/></w:sectPr></w:pPr></w:p>',
    ancho, alto, orient))
end

-- Anchos en cm. Area util A4 con los margenes de plantilla.docx: 17.8 cm en vertical;
-- en la pagina apaisada el diagrama E-R queda limitado por el alto (unos 13 cm).
local anchos = {
  er = '16cm',
  login = '9cm',
  form = '16cm',
  captura = '17.5cm',
}

function Image(img)
  for _, clase in ipairs({ 'er', 'login', 'form', 'captura' }) do
    if img.classes:includes(clase) then
      img.attributes.width = anchos[clase]
      img.attributes.height = nil
      return img
    end
  end
end

local function esPagina(b)
  return b.t == 'Div' and b.classes:includes('pagina')
end

local function esHorizontal(b)
  return esPagina(b) and b.classes:includes('horizontal')
end

-- Recorre las paginas en orden para decidir como termina cada una:
--   * antes de una pagina apaisada: fin de seccion vertical;
--   * la pagina apaisada: fin de seccion apaisada;
--   * el resto: salto de pagina, salvo la ultima.
function Pandoc(doc)
  local bloques = doc.blocks
  local salida = {}
  for i, b in ipairs(bloques) do
    if esPagina(b) then
      for _, interno in ipairs(b.content) do
        table.insert(salida, interno)
      end
      local siguiente = bloques[i + 1]
      if esHorizontal(b) then
        table.insert(salida, finSeccion(true))
      elseif siguiente and esHorizontal(siguiente) then
        table.insert(salida, finSeccion(false))
      elseif siguiente then
        table.insert(salida, salto)
      end
    else
      table.insert(salida, b)
    end
  end
  doc.blocks = salida
  return doc
end
