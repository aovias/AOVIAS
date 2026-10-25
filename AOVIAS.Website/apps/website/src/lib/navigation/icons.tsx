import {
  ArrowRight,
  BookOpen,
  Box,
  Building2,
  ExternalLink,
  FileText,
  Layers,
  Search,
  Shield,
  Users,
  type LucideIcon,
} from 'lucide-react';

import type { NavIconName } from '@/config/navigation/types';

const iconMap: Record<NavIconName, LucideIcon> = {
  box: Box,
  layers: Layers,
  shield: Shield,
  book: BookOpen,
  building: Building2,
  users: Users,
  file: FileText,
  external: ExternalLink,
  search: Search,
  arrowRight: ArrowRight,
};

export function getNavIcon(name: NavIconName | undefined): LucideIcon | null {
  if (!name) return null;
  return iconMap[name];
}
