/**
 * Backend DTO'larının aynası. Enum'lar sunucuda metin olarak serileşir
 * (JsonStringEnumConverter), bu yüzden burada birleşim tipleri kullanılıyor —
 * `enum` yerine union: tsconfig'te `erasableSyntaxOnly` açık.
 */

export type UserRole = 'SuperAdmin' | 'ProgramManager' | 'StartupUser' | 'DecisionMaker'

export type Sector =
  | 'Other' | 'Defense' | 'Health' | 'Software' | 'Energy' | 'Agriculture'
  | 'Education' | 'Finance' | 'Mobility' | 'Space' | 'Manufacturing'

export type StartupStatus = 'Active' | 'Inactive' | 'Graduated' | 'Exited' | 'Acquired'

export type ProgramType =
  | 'Other' | 'PreIncubation' | 'Incubation' | 'Acceleration'
  | 'Competition' | 'Event' | 'Training'

export type ParticipationStatus =
  | 'Applied' | 'Accepted' | 'InProgress' | 'Completed' | 'Graduated' | 'Dropped'

export type TimelineEntryKind =
  | 'Founding' | 'ProgramJoined' | 'ProgramCompleted' | 'Milestone'
  | 'Investment' | 'Grant' | 'Revenue' | 'Export' | 'Award'

export type StartupSort = 'Name' | 'Newest' | 'MostInvestment'

// --- Oturum ---------------------------------------------------------------

export type AssignedProgram = { id: string; name: string }

export type SessionPermissions = {
  canManageStartups: boolean
  /** Program tanımı — kapsamın kendisi, yalnızca Süper Yönetici. */
  canManagePrograms: boolean
  /** Dönem ve katılım işlemleri; Program Yöneticisi kendi programında. */
  canManageProgramTerms: boolean
  canReviewApprovals: boolean
  canManageUsers: boolean
  canSeeExactAmounts: boolean
  canSeeContactDetails: boolean
  mustSubmitForApproval: boolean
}

export type SessionUser = {
  id: string
  email: string
  fullName: string
  role: UserRole
  startupId: string | null
  startupName: string | null
  programs: AssignedProgram[]
  /** Yönetici şifre atadı: kullanıcı değiştirmeden başka ekrana geçemez. */
  mustChangePassword: boolean
  permissions: SessionPermissions
}

export type LoginResponse = {
  accessToken: string
  expiresAt: string
  user: SessionUser
}

// --- Sayfalama ------------------------------------------------------------

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNext: boolean
}

// --- Girişim listesi ------------------------------------------------------

export type StartupListItem = {
  id: string
  name: string
  sector: Sector
  city: string | null
  status: StartupStatus
  logoUrl: string | null
  foundedOn: string | null
  technologyAreas: string[]
  latestProgramName: string | null
  programCount: number
  achievementCount: number
  /** `amountsVisible` false ise maskelenmiş, true ve null ise kayıt yok. */
  totalInvestment: number | null
  amountsVisible: boolean
  currency: string
}

// --- Girişim kartı --------------------------------------------------------

export type CardTeamMember = {
  id: string
  fullName: string
  title: string | null
  email: string | null
  phone: string | null
  linkedInUrl: string | null
  isFounder: boolean
  joinedOn: string | null
}

export type CardParticipation = {
  id: string
  programId: string
  programName: string
  programType: ProgramType
  coordinatorship: string | null
  programTermId: string
  termName: string
  status: ParticipationStatus
  joinedOn: string
  leftOn: string | null
  notes: string | null
}

export type CardAchievementSummary = {
  totalCount: number
  investmentRoundCount: number
  awardCount: number
  totalInvestment: number | null
  totalGrant: number | null
  latestAnnualRevenue: number | null
  latestRevenueYear: number | null
  totalExport: number | null
  currency: string
}

/** Hangi alan grubunun görülebildiğini bildirir; arayüz kilit simgesi için kullanır. */
export type CardVisibility = {
  contactDetails: boolean
  taxNumber: boolean
  exactAmounts: boolean
  teamPersonalData: boolean
  documents: boolean
}

export type StartupCard = {
  id: string
  name: string
  legalName: string | null
  taxNumber: string | null
  foundedOn: string | null
  sector: Sector
  technologyAreas: string[]
  productDescription: string | null
  website: string | null
  logoUrl: string | null
  city: string | null
  contactEmail: string | null
  contactPhone: string | null
  status: StartupStatus
  team: CardTeamMember[]
  programs: CardParticipation[]
  achievements: CardAchievementSummary
  documentCount: number
  milestoneCount: number
  createdAt: string
  updatedAt: string | null
  visibility: CardVisibility
}

// --- Zaman çizelgesi ------------------------------------------------------

export type TimelineEntry = {
  kind: TimelineEntryKind
  occurredOn: string
  title: string
  description: string | null
  badge: string | null
  amount: number | null
  currency: string | null
  isVerified: boolean
  sourceId: string | null
}

export type StartupTimeline = {
  startupId: string
  startupName: string
  exactAmountsVisible: boolean
  entries: TimelineEntry[]
}

// --- Programlar -----------------------------------------------------------

export type ProgramTerm = {
  id: string
  name: string
  startsOn: string
  endsOn: string | null
  participantCount: number
}

export type Program = {
  id: string
  name: string
  type: ProgramType
  coordinatorship: string | null
  description: string | null
  startupCount: number
  terms: ProgramTerm[]
}

// --- Yazma gövdeleri ------------------------------------------------------

export type StartupWriteModel = {
  name: string
  legalName?: string | null
  taxNumber?: string | null
  foundedOn?: string | null
  sector: Sector
  technologyAreas?: string[] | null
  productDescription?: string | null
  website?: string | null
  logoUrl?: string | null
  city?: string | null
  contactEmail?: string | null
  contactPhone?: string | null
  status?: StartupStatus | null
}

/** Katılım gövdesi; dönem kimliği `Program.terms` içinden seçiliyor. */
export type AddParticipationBody = {
  startupId: string
  programTermId: string
  status: ParticipationStatus
  joinedOn: string
  leftOn?: string | null
  notes?: string | null
}

export type TeamMemberWriteModel = {
  fullName: string
  title?: string | null
  email?: string | null
  phone?: string | null
  linkedInUrl?: string | null
  isFounder: boolean
  joinedOn?: string | null
}

// --- Onay akışı (MVP #3) --------------------------------------------------

export type ChangeTargetType = 'Startup' | 'TeamMember' | 'Achievement' | 'Document'
export type ChangeOperation = 'Create' | 'Update' | 'Delete'
export type ChangeRequestStatus = 'Pending' | 'Approved' | 'Rejected'

export type ChangeRequestListItem = {
  id: string
  startupId: string
  startupName: string
  targetType: ChangeTargetType
  operation: ChangeOperation
  targetId: string | null
  /** Sunucuda üretilen Türkçe etiket: "Girişim profili", "Ekip üyesi". */
  targetLabel: string
  operationLabel: string
  changedFieldCount: number
  submittedByName: string
  submittedAt: string
  waitingDays: number
  status: ChangeRequestStatus
  reviewedByName: string | null
  reviewedAt: string | null
  reviewNote: string | null
}

/**
 * Kuyruk yanıtı. Sayımlar durum süzgecinden bağımsız gelir; sekme başlıkları
 * "Bekleyen (6)" gibi sayıları kendi filtrelerini uygulamadan gösterebilsin.
 */
export type ChangeRequestQueue = {
  page: PagedResult<ChangeRequestListItem>
  pendingCount: number
  approvedCount: number
  rejectedCount: number
}

/**
 * Diff satırı. `masked` true ise değer sunucuda hiç yollanmamıştır; `changed`
 * bilgisi yine gelir — yetkisiz kullanıcı "bir şey değişiyor" bilgisine sahip
 * olmalı, değerin kendisine değil.
 */
export type DiffField = {
  field: string
  label: string
  before: string | null
  after: string | null
  changed: boolean
  masked: boolean
}

export type ChangeRequestDetail = {
  id: string
  startupId: string
  startupName: string
  targetType: ChangeTargetType
  operation: ChangeOperation
  targetId: string | null
  targetLabel: string
  operationLabel: string
  submittedByName: string
  submittedAt: string
  status: ChangeRequestStatus
  reviewedByName: string | null
  reviewedAt: string | null
  reviewNote: string | null
  /** Karar düğmelerinin görünürlüğü; gerçek yetki kontrolü sunucuda. */
  canReview: boolean
  /** Gövde okunamıyorsa onay verilemez; ekran boş diff yerine durumu söyler. */
  isReadable: boolean
  changedFieldCount: number
  fields: DiffField[]
}

export type SubmitChangeRequestBody = {
  targetType: ChangeTargetType
  operation: ChangeOperation
  targetId?: string | null
  startup?: StartupWriteModel | null
  teamMember?: TeamMemberWriteModel | null
  achievement?: AchievementWriteModel | null
}

export type ReviewChangeRequestResult = {
  id: string
  status: ChangeRequestStatus
  reviewedAt: string
  reviewNote: string | null
  appliedEntityType: string | null
  appliedEntityId: string | null
}

// --- Kullanıcı yönetimi ---------------------------------------------------

export type UserProgram = { id: string; name: string }

export type UserRow = {
  id: string
  email: string
  fullName: string
  role: UserRole
  roleLabel: string
  startupId: string | null
  startupName: string | null
  programs: UserProgram[]
  isActive: boolean
  lastLoginAt: string | null
  createdAt: string
}

export type CreateUserBody = {
  email: string
  fullName: string
  role: UserRole
  password: string
  startupId?: string | null
  programIds?: string[] | null
}

/** E-posta bilinçli olarak yok: kimliğin çapası, güncellemeyle taşınmıyor. */
export type UpdateUserBody = {
  fullName: string
  role: UserRole
  startupId?: string | null
  programIds?: string[] | null
  isActive: boolean
}

// --- Denetim izi ----------------------------------------------------------

export type AuditLogRow = {
  id: string
  action: string
  entityType: string
  entityId: string | null
  /** Başarısız giriş denemesinde null: kimlik doğrulanmamıştır. */
  actorUserId: string | null
  actorName: string
  actorRole: UserRole | null
  ipAddress: string | null
  userAgent: string | null
  occurredAt: string
  /** Ham JSON: iz kanıt niteliği taşıdığı için biçimlendirilmeden gösterilir. */
  beforeJson: string | null
  afterJson: string | null
}

// --- Program yönetimi (Dalga 1) -------------------------------------------

export type ProgramWriteModel = {
  name: string
  type: ProgramType
  coordinatorship?: string | null
  description?: string | null
}

export type ProgramSummary = {
  id: string
  name: string
  type: ProgramType
  coordinatorship: string | null
  description: string | null
}

export type ProgramTermWriteModel = {
  name: string
  startsOn: string
  endsOn?: string | null
}

/** Soft delete zincirinin raporu: program kapatınca ne kapandı. */
export type DeleteProgramResult = {
  id: string
  name: string
  terms: number
  participations: number
  managerAssignments: number
}

/** Katılım düzeltmesi; girişim ve dönem taşınmıyor (kaldır + yeniden ekle). */
export type UpdateParticipationBody = {
  status: ParticipationStatus
  joinedOn: string
  leftOn?: string | null
  notes?: string | null
}

// --- Şifre kurtarma (Dalga 1) ---------------------------------------------

export type ForgotPasswordBody = { email: string }
export type ResetPasswordBody = { token: string; newPassword: string }
export type ChangePasswordBody = { currentPassword: string; newPassword: string }

// --- Girişim silme --------------------------------------------------------

/** Soft delete zincirinin raporu: hangi bağlı kayıt kaç adet pasife alındı. */
export type DeleteStartupResult = {
  id: string
  name: string
  teamMembers: number
  participations: number
  milestones: number
  achievements: number
  documents: number
  changeRequests: number
  deactivatedUsers: number
}

// --- Başarı ve finans kayıtları (MVP #4) ----------------------------------

export type AchievementKind = 'Revenue' | 'Export' | 'Investment' | 'Grant' | 'Award'

export type InvestmentRoundType =
  | 'Other'
  | 'Angel'
  | 'PreSeed'
  | 'Seed'
  | 'SeriesA'
  | 'SeriesB'
  | 'SeriesC'
  | 'Debt'

export type GrantInstitution =
  | 'Other'
  | 'Tubitak'
  | 'Kosgeb'
  | 'Teknofest'
  | 'EuropeanUnion'
  | 'Ministry'
  | 'DevelopmentAgency'

/**
 * Tek başarı/finans kaydı. `amount` iki nedenle `null` olabilir: kayıt tutar
 * taşımıyor (ödül) ya da tutarı görme yetkisi yok. Ayrımı `amountMasked`
 * yapıyor — arayüz maskelenen tutarı `0 ₺` diye göstermemeli.
 */
export type Achievement = {
  id: string
  startupId: string
  kind: AchievementKind
  kindLabel: string
  occurredOn: string
  title: string
  note: string | null
  amount: number | null
  currency: string | null
  amountMasked: boolean
  fiscalYear: number | null
  quarter: number | null
  periodLabel: string | null
  roundType: InvestmentRoundType | null
  roundTypeLabel: string | null
  valuation: number | null
  investorNames: string[]
  institution: GrantInstitution | null
  institutionLabel: string | null
  programName: string | null
  awardName: string | null
  organization: string | null
  rank: number | null
  targetCountries: string[]
  isVerified: boolean
  verifiedAt: string | null
  createdAt: string
  updatedAt: string | null
}

export type AchievementList = {
  startupId: string
  exactAmountsVisible: boolean
  items: Achievement[]
}

/** Ekleme/güncelleme gövdesi; ilgisiz alanlar `null` gönderilir. */
export type AchievementWriteModel = {
  kind: AchievementKind
  occurredOn: string
  note: string | null
  amount: number | null
  currency: string | null
  fiscalYear: number | null
  quarter: number | null
  roundType: InvestmentRoundType | null
  valuation: number | null
  investorNames: string[] | null
  institution: GrantInstitution | null
  programName: string | null
  awardName: string | null
  organization: string | null
  rank: number | null
  targetCountries: string[] | null
}

// --- Dokümanlar (MVP #4) --------------------------------------------------

export type DocumentType =
  | 'Other'
  | 'PitchDeck'
  | 'Financials'
  | 'Incorporation'
  | 'Patent'
  | 'Report'
  | 'Contract'

export type StartupDocument = {
  id: string
  startupId: string
  type: DocumentType
  typeLabel: string
  fileName: string
  contentType: string
  sizeBytes: number
  sizeLabel: string
  uploadedByUserId: string
  uploadedByName: string | null
  uploadedAt: string
}

export type DocumentList = {
  startupId: string
  items: StartupDocument[]
}

/**
 * Yükleme sonucu. `applied` yanlışsa dosya depoya alındı ama kayıt henüz yok:
 * girişim kullanıcısının yüklemesi onay kuyruğunda bekliyor.
 */
export type DocumentUploadResult = {
  applied: boolean
  document: StartupDocument | null
  changeRequestId: string | null
  message: string
}

// --- Ekosistem panosu (Faz 5) ---------------------------------------------

/**
 * `amountsVisible` agregat düzeydeki tutar yetkisidir. Karar Verici için
 * `true` gelir (ekosistem toplamını görür) ama `topByInvestment` satırlarındaki
 * `investment` yine de `null` olur — tekil girişimin tutarı satır düzeyi
 * hassas veridir. İki bayrağın aynı olmaması bilinçli.
 */
export type EcosystemStats = {
  totals: EcosystemTotals
  bySector: CountSlice[]
  byStatus: CountSlice[]
  byCity: CountSlice[]
  byProgram: CountSlice[]
  investmentByRound: MoneySlice[]
  investmentByYear: MoneySlice[]
  revenueByYear: MoneySlice[]
  topByInvestment: TopStartupSlice[]
  amountsVisible: boolean
  currency: string
  generatedAt: string
}

export type EcosystemTotals = {
  startups: number
  activeStartups: number
  graduatedStartups: number
  programs: number
  participations: number
  achievements: number
  investedStartups: number
  totalInvestment: number | null
  totalGrant: number | null
  totalExport: number | null
  latestRevenueYear: number | null
  latestRevenue: number | null
}

export type CountSlice = { key: string; label: string; count: number }
export type MoneySlice = { key: string; label: string; count: number; total: number | null }

/** Listeye yalnızca yatırım alan girişimler girer; `investment: null` ⇒ maskeli. */
export type TopStartupSlice = {
  startupId: string
  name: string
  sectorLabel: string
  investment: number | null
}

// --- AI karar destek (Faz 5) ----------------------------------------------

/** `Local`: anahtar tanımlı değil, yanıt yerel anahtar sözcük planlayıcısından. */
export type AssistantMode = 'Model' | 'Local'

export type AssistantSource = { tool: string; summary: string }

export type AssistantAnswer = {
  question: string
  answer: string
  sources: AssistantSource[]
  mode: AssistantMode
  modelName: string
  answeredAt: string
}

export type StartupSummary = {
  startupId: string
  name: string
  summary: string
  highlights: string[]
  mode: AssistantMode
  modelName: string
  exactAmountsVisible: boolean
  generatedAt: string
}
