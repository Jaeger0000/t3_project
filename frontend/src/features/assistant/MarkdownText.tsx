import { Fragment, type ReactNode } from 'react'

/**
 * Dil modelinin cevabını çizen küçük Markdown görüntüleyici.
 *
 * Neden kütüphane değil: model çıktısı **güvenilmeyen metin**. Hazır
 * çözümlerin çoğu HTML'i olduğu gibi basıyor (`dangerouslySetInnerHTML`) ve o
 * yol bu ekranda bir XSS kapısıdır — cevabın içeriğini kullanıcı değil, dışarıdaki
 * bir model belirliyor. Burada hiç HTML üretilmiyor: metin React düğümlerine
 * çevriliyor, yani `<script>` yazan bir cevap ekranda düz yazı olarak kalır.
 *
 * Neden gerekli: model tablo döndürüyor. Ham metin olarak basıldığında ekranda
 * `|---|---------|` ayraç satırları ve boş hücreler kalıyordu — kullanıcının
 * "girişim adını verdi ama yanı bir sürü boş yer" dediği görüntü tam olarak
 * buydu. Desteklenen alt küme kasıtlı olarak dar: başlık, kalın/eğik/kod,
 * madde ve numaralı liste, yatay çizgi, tablo. Gerisi paragraf olarak geçer —
 * tanınmayan bir işaret ekranı bozmaz, olduğu gibi görünür.
 */
export default function MarkdownText({
  text,
  className,
  'data-testid': testId,
}: {
  text: string
  className?: string
  'data-testid'?: string
}) {
  return (
    // Bloklar arası boşluk kapta: her blok kendi kenar boşluğunu
    // taşısaydı ilk blok da üstten boşluk alırdı.
    <div className={`flex flex-col gap-2 ${className ?? ''}`} data-testid={testId}>
      {renderBlocks(text)}
    </div>
  )
}

const TABLE_SEPARATOR = /^\s*\|?\s*:?-{2,}:?\s*(\|\s*:?-{2,}:?\s*)*\|?\s*$/

function renderBlocks(text: string): ReactNode[] {
  const lines = text.replace(/\r\n/g, '\n').split('\n')
  const blocks: ReactNode[] = []
  let paragraph: string[] = []
  let index = 0

  const flushParagraph = () => {
    if (paragraph.length === 0) return
    blocks.push(
      <p key={`p-${blocks.length}`} className="whitespace-pre-line">
        {renderInline(paragraph.join('\n'))}
      </p>,
    )
    paragraph = []
  }

  while (index < lines.length) {
    const line = lines[index]

    if (line.trim().length === 0) {
      flushParagraph()
      index += 1
      continue
    }

    // --- Tablo: başlık satırı + hemen ardından ayraç satırı ---------------
    if (line.trim().startsWith('|') && TABLE_SEPARATOR.test(lines[index + 1] ?? '')) {
      const header = splitRow(line)
      const rows: string[][] = []
      index += 2
      while (index < lines.length && lines[index].trim().startsWith('|')) {
        rows.push(splitRow(lines[index]))
        index += 1
      }
      flushParagraph()
      blocks.push(
        // Geniş içerik kendi kabında kayar: sayfanın gövdesi yatay kaymaz
        // (375 px'te tablo satırı taşıyordu).
        <div key={`t-${blocks.length}`} className="-mx-1 overflow-x-auto px-1">
          <table className="w-full min-w-max border-collapse text-left text-[0.9em]">
            <thead>
              <tr className="border-b border-stone-300 dark:border-stone-700">
                {header.map((cell, cellIndex) => (
                  <th key={cellIndex} className="px-2 py-1.5 font-semibold whitespace-nowrap">
                    {renderInline(cell)}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, rowIndex) => (
                <tr
                  key={rowIndex}
                  className="border-b border-stone-200 last:border-0 dark:border-stone-800"
                >
                  {header.map((_, cellIndex) => (
                    <td key={cellIndex} className="px-2 py-1.5 align-top">
                      {renderInline(row[cellIndex] ?? '')}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>,
      )
      continue
    }

    // --- Yatay çizgi ------------------------------------------------------
    if (/^\s*(-{3,}|\*{3,}|_{3,})\s*$/.test(line)) {
      flushParagraph()
      blocks.push(
        <hr key={`hr-${blocks.length}`} className="border-stone-200 dark:border-stone-800" />,
      )
      index += 1
      continue
    }

    // --- Başlık -----------------------------------------------------------
    const heading = line.match(/^\s*(#{1,4})\s+(.*)$/)
    if (heading) {
      flushParagraph()
      blocks.push(
        <p
          key={`h-${blocks.length}`}
          className="font-semibold text-stone-900 dark:text-stone-100"
        >
          {renderInline(heading[2])}
        </p>,
      )
      index += 1
      continue
    }

    // --- Listeler ---------------------------------------------------------
    const ordered = /^\s*\d+[.)]\s+/
    const unordered = /^\s*[-*•]\s+/
    if (ordered.test(line) || unordered.test(line)) {
      const isOrdered = ordered.test(line)
      const items: string[] = []
      while (
        index < lines.length &&
        (isOrdered ? ordered.test(lines[index]) : unordered.test(lines[index]))
      ) {
        items.push(lines[index].replace(isOrdered ? ordered : unordered, ''))
        index += 1
      }
      flushParagraph()
      const ListTag = isOrdered ? 'ol' : 'ul'
      blocks.push(
        <ListTag
          key={`l-${blocks.length}`}
          className={`ml-4 flex flex-col gap-1 ${
            isOrdered ? 'list-decimal' : 'list-disc'
          }`}
        >
          {items.map((item, itemIndex) => (
            <li key={itemIndex}>{renderInline(item)}</li>
          ))}
        </ListTag>,
      )
      continue
    }

    paragraph.push(line)
    index += 1
  }

  flushParagraph()
  return blocks
}

/** `| a | b |` → `['a', 'b']`; baştaki/sondaki boş hücre atılır. */
function splitRow(line: string): string[] {
  return line
    .trim()
    .replace(/^\|/, '')
    .replace(/\|$/, '')
    .split('|')
    .map((cell) => cell.trim())
}

const INLINE = /(\*\*[^*\n]+\*\*|__[^_\n]+__|`[^`\n]+`|\*[^*\n]+\*)/g

function renderInline(text: string): ReactNode[] {
  return text.split(INLINE).map((piece, index) => {
    if (!piece) return null

    if (piece.startsWith('**') && piece.endsWith('**')) {
      return <strong key={index}>{piece.slice(2, -2)}</strong>
    }
    if (piece.startsWith('__') && piece.endsWith('__')) {
      return <strong key={index}>{piece.slice(2, -2)}</strong>
    }
    if (piece.startsWith('`') && piece.endsWith('`')) {
      return (
        <code
          key={index}
          className="rounded bg-stone-200/70 px-1 py-0.5 text-[0.9em] dark:bg-stone-800"
        >
          {piece.slice(1, -1)}
        </code>
      )
    }
    if (piece.startsWith('*') && piece.endsWith('*')) {
      return <em key={index}>{piece.slice(1, -1)}</em>
    }
    return <Fragment key={index}>{piece}</Fragment>
  })
}
