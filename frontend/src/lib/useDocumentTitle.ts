import { useEffect } from 'react'

/**
 * Sekme başlığını sayfanın H1'iyle aynı metne çeker. Üç girişim kartını üç
 * sekmede açan program yöneticisi hangisinin hangisi olduğunu sekmeden
 * ayırt edemiyordu — 55 render'ın hepsinde başlık "frontend" yazıyordu.
 */
export function useDocumentTitle(title: string | undefined) {
  useEffect(() => {
    document.title = title
      ? `${title} · T3 Girişim Ekosistemi`
      : 'T3 Girişim Ekosistemi Yönetim Sistemi'
  }, [title])
}
