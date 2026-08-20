import cardStyles from '../../design-system/components/Card.module.css'

// Used both by the jump bar's own links and by clicking an item in the profile preview — the
// flash is what tells the user "this is the card that just scrolled into view."
export function scrollToSection(id: string) {
  const element = document.getElementById(id)
  if (!element) return
  element.classList.remove(cardStyles.flash)
  void element.offsetWidth
  element.classList.add(cardStyles.flash)
  element.scrollIntoView?.({ behavior: 'smooth', block: 'start' })
}
