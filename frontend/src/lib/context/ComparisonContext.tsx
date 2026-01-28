'use client';

import { createContext, useContext, useState, ReactNode } from 'react';
import { VehicleDocument } from '@/lib/api/types';

interface ComparisonContextType {
  selectedVehicles: VehicleDocument[];
  toggleVehicle: (vehicle: VehicleDocument) => void;
  clearComparison: () => void;
  isSelected: (vehicleId: string) => boolean;
}

const ComparisonContext = createContext<ComparisonContextType | undefined>(undefined);

export function ComparisonProvider({ children }: { children: ReactNode }) {
  const [selectedVehicles, setSelectedVehicles] = useState<VehicleDocument[]>([]);

  const toggleVehicle = (vehicle: VehicleDocument) => {
    setSelectedVehicles((prev) => {
      const exists = prev.find((v) => v.id === vehicle.id);
      if (exists) {
        // Remove if already selected
        return prev.filter((v) => v.id !== vehicle.id);
      } else {
        // Add if not selected (max 3)
        if (prev.length >= 3) {
          return prev;
        }
        return [...prev, vehicle];
      }
    });
  };

  const clearComparison = () => {
    setSelectedVehicles([]);
  };

  const isSelected = (vehicleId: string) => {
    return selectedVehicles.some((v) => v.id === vehicleId);
  };

  return (
    <ComparisonContext.Provider
      value={{ selectedVehicles, toggleVehicle, clearComparison, isSelected }}
    >
      {children}
    </ComparisonContext.Provider>
  );
}

export function useComparison() {
  const context = useContext(ComparisonContext);
  if (context === undefined) {
    throw new Error('useComparison must be used within a ComparisonProvider');
  }
  return context;
}
