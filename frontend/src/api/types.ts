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

export type TeamMemberWriteModel = {
  fullName: string
  title?: string | null
  email?: string | null
  phone?: string | null
  linkedInUrl?: string | null
  isFounder: boolean
  joinedOn?: string | null
}
