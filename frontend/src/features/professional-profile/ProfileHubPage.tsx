import { useState } from 'react'
import { Tabs } from '../../design-system/components/Tabs'
import { CVPresentationDetailPage } from '../cv-presentations/CVPresentationDetailPage'
import { CVPresentationForm } from '../cv-presentations/CVPresentationForm'
import { CVPresentationsListPage } from '../cv-presentations/CVPresentationsListPage'
import { ProfessionalProfilePage } from './ProfessionalProfilePage'
import styles from './ProfileHubPage.module.css'

type HubTab = 'profile' | 'presentations'

type PresentationsView = { name: 'list' } | { name: 'detail'; id: string } | { name: 'new' }

const HUB_TABS = [
  { key: 'profile', label: 'Profile' },
  { key: 'presentations', label: 'CV presentations' },
]

// components.md AppShell destination 3, "Professional profile & CVs" — owns the Profile/CV
// presentations split locally rather than pushing it into App.tsx's global View union, the same
// way StudyItemDetailPage nests its own isEditing state instead of a global view case.
export function ProfileHubPage() {
  const [hubTab, setHubTab] = useState<HubTab>('profile')
  const [presentationsView, setPresentationsView] = useState<PresentationsView>({ name: 'list' })

  return (
    <div className={styles.page}>
      <Tabs tabs={HUB_TABS} activeTab={hubTab} onChange={(key) => setHubTab(key as HubTab)} aria-label="Professional profile sections" />

      <div id={`tabpanel-${hubTab}`} role="tabpanel" aria-labelledby={`tab-${hubTab}`}>
        {hubTab === 'profile' && <ProfessionalProfilePage />}

        {hubTab === 'presentations' && presentationsView.name === 'list' && (
          <CVPresentationsListPage
            onSelectPresentation={(id) => setPresentationsView({ name: 'detail', id })}
            onCreateNew={() => setPresentationsView({ name: 'new' })}
          />
        )}

        {hubTab === 'presentations' && presentationsView.name === 'detail' && (
          <CVPresentationDetailPage
            key={presentationsView.id}
            presentationId={presentationsView.id}
            onBack={() => setPresentationsView({ name: 'list' })}
            onDeleted={() => setPresentationsView({ name: 'list' })}
          />
        )}

        {hubTab === 'presentations' && presentationsView.name === 'new' && (
          <CVPresentationForm
            mode="create"
            onCreated={(id) => setPresentationsView({ name: 'detail', id })}
            onCancel={() => setPresentationsView({ name: 'list' })}
            onGoToProfile={() => setHubTab('profile')}
          />
        )}
      </div>
    </div>
  )
}
