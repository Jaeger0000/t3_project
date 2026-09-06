import { Link } from 'react-router-dom'
import { useAuth } from '@/lib/auth'
import { roleLabels } from '@/lib/labels'
import { Badge, Card, DataRow } from '@/components/ui'
import { useDocumentTitle } from '@/lib/useDocumentTitle'

/**
 * Kullanıcının kendi hesap bilgilerini gördüğü ekran.
 *
 * Ad-soyad ve e-posta yalnızca SuperAdmin'in kullanıcı yönetimi ucundan
 * değişir (e-posta tasarım gereği hiç değişmez — kimlik alanı budur);
 * kullanıcının kendi kendine değiştirebildiği tek şey şifresi, o yüzden
 * burada salt bilgi + "şifre değiştir" bağlantısı var.
 */
export default function ProfilePage() {
  useDocumentTitle('Profilim')
  const { session } = useAuth()

  if (!session) return null

  return (
    <div className="mx-auto flex max-w-lg flex-col gap-6">
      <header>
        <h1 className="text-2xl font-bold text-stone-900 dark:text-stone-50">Profilim</h1>
        <p className="mt-1 text-sm text-stone-500">Hesap bilgileriniz.</p>
      </header>

      <Card className="p-5">
        <div className="flex flex-col gap-3">
          <DataRow label="Ad soyad">{session.fullName}</DataRow>
          <DataRow label="E-posta">{session.email}</DataRow>
          <DataRow label="Rol">
            <Badge tone="bg-brand-100 text-brand-800 dark:bg-brand-950 dark:text-brand-200">
              {roleLabels[session.role]}
            </Badge>
          </DataRow>
          {session.startupName ? (
            <DataRow label="Girişim">{session.startupName}</DataRow>
          ) : null}
          {session.programs.length > 0 ? (
            <DataRow label="Programlar">{session.programs.map((p) => p.name).join(', ')}</DataRow>
          ) : null}
        </div>
      </Card>

      <Card className="p-5">
        <h2 className="font-semibold text-stone-900 dark:text-stone-100">Şifre</h2>
        <p className="mt-1 text-sm text-stone-500">
          Ad-soyad ve e-posta değişikliği yönetici onayı gerektirir; şifrenizi ise
          buradan kendiniz güncelleyebilirsiniz.
        </p>
        <Link
          to="/sifre-degistir"
          className="mt-3 inline-flex items-center justify-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-600 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600"
        >
          Şifre değiştir
        </Link>
      </Card>
    </div>
  )
}
