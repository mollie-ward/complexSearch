'use client';

import { useState } from 'react';
import { VehicleResult } from '@/lib/api/types';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { ChevronDown, ChevronUp, Info } from 'lucide-react';

interface VehicleCardProps {
  result: VehicleResult;
  rank: number;
}

export function VehicleCard({ result, rank }: VehicleCardProps) {
  const [showExplanation, setShowExplanation] = useState(false);
  const { vehicle, score, scoreBreakdown } = result;

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(price);
  };

  const formatMileage = (mileage: number) => {
    return new Intl.NumberFormat('en-GB').format(mileage);
  };

  const getYear = (date?: string) => {
    if (!date) return 'N/A';
    return new Date(date).getFullYear().toString();
  };

  const relevancePercentage = Math.round(score * 100);

  // Get top 5 features
  const visibleFeatures = vehicle.features?.slice(0, 5) || [];
  const remainingFeaturesCount = (vehicle.features?.length || 0) - 5;

  return (
    <Card className="h-full flex flex-col" data-testid="vehicle-card">
      <CardHeader>
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-2">
              <Badge variant="secondary" className="font-bold">
                #{rank}
              </Badge>
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <div className="flex items-center gap-1 text-sm font-medium cursor-help">
                      <span>Match: {relevancePercentage}%</span>
                      <Info className="h-3 w-3" />
                    </div>
                  </TooltipTrigger>
                  <TooltipContent>
                    <div className="space-y-1">
                      <div className="font-semibold">Score Breakdown:</div>
                      {scoreBreakdown && (
                        <>
                          <div>Semantic: {Math.round(scoreBreakdown.semanticScore * 100)}%</div>
                          <div>Exact Match: {Math.round(scoreBreakdown.exactMatchScore * 100)}%</div>
                          {scoreBreakdown.keywordScore !== undefined && (
                            <div>Keyword: {Math.round(scoreBreakdown.keywordScore * 100)}%</div>
                          )}
                          <div className="border-t pt-1 mt-1">
                            Overall: {Math.round(scoreBreakdown.finalScore * 100)}%
                          </div>
                        </>
                      )}
                    </div>
                  </TooltipContent>
                </Tooltip>
              </TooltipProvider>
            </div>
            <CardTitle className="text-xl">
              {vehicle.make} {vehicle.model}
            </CardTitle>
            {vehicle.derivative && (
              <CardDescription className="mt-1">{vehicle.derivative}</CardDescription>
            )}
          </div>
          <div className="text-right">
            <div className="text-2xl font-bold">{formatPrice(vehicle.price)}</div>
          </div>
        </div>
      </CardHeader>

      <CardContent className="flex-1">
        <div className="grid grid-cols-2 gap-2 text-sm mb-4">
          <div>
            <span className="text-muted-foreground">Mileage:</span>{' '}
            <span className="font-medium">{formatMileage(vehicle.mileage)} mi</span>
          </div>
          <div>
            <span className="text-muted-foreground">Year:</span>{' '}
            <span className="font-medium">{getYear(vehicle.registrationDate)}</span>
          </div>
          <div>
            <span className="text-muted-foreground">Transmission:</span>{' '}
            <span className="font-medium">{vehicle.transmissionType}</span>
          </div>
          <div>
            <span className="text-muted-foreground">Fuel:</span>{' '}
            <span className="font-medium">{vehicle.fuelType}</span>
          </div>
          <div className="col-span-2">
            <span className="text-muted-foreground">Location:</span>{' '}
            <span className="font-medium">{vehicle.saleLocation}</span>
          </div>
        </div>

        {visibleFeatures.length > 0 && (
          <div className="mb-4">
            <div className="flex flex-wrap gap-1">
              {visibleFeatures.map((feature) => (
                <Badge key={`${vehicle.id}-${feature}`} variant="outline" className="text-xs">
                  {feature}
                </Badge>
              ))}
              {remainingFeaturesCount > 0 && (
                <Badge variant="outline" className="text-xs">
                  +{remainingFeaturesCount} more
                </Badge>
              )}
            </div>
          </div>
        )}

        {vehicle.serviceHistoryPresent && (
          <div className="mb-4">
            <Badge variant="default" className="bg-green-600">
              Full Service History
            </Badge>
          </div>
        )}

        {vehicle.description && (
          <p className="text-sm text-muted-foreground line-clamp-2 mb-4">
            {vehicle.description}
          </p>
        )}

        {showExplanation && (
          <div className="mt-4 p-3 bg-muted rounded-md">
            <h4 className="font-semibold text-sm mb-2">Match Explanation</h4>
            <div className="text-sm space-y-1">
              <p>This vehicle matched your search criteria based on:</p>
              <ul className="list-disc list-inside space-y-1 text-muted-foreground">
                <li>
                  <strong>Overall relevance:</strong> {relevancePercentage}% match
                </li>
                {scoreBreakdown && (
                  <>
                    {scoreBreakdown.semanticScore > 0.3 && (
                      <li>Strong semantic similarity to your query</li>
                    )}
                    {scoreBreakdown.exactMatchScore > 0.3 && (
                      <li>Exact matches on key criteria</li>
                    )}
                  </>
                )}
                {vehicle.serviceHistoryPresent && (
                  <li>Full service history available</li>
                )}
                {vehicle.mileage < 50000 && (
                  <li>Low mileage ({formatMileage(vehicle.mileage)} miles)</li>
                )}
              </ul>
            </div>
          </div>
        )}
      </CardContent>

      <CardFooter className="flex justify-between gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowExplanation(!showExplanation)}
        >
          {showExplanation ? (
            <>
              <ChevronUp className="mr-1 h-4 w-4" />
              Hide explanation
            </>
          ) : (
            <>
              <ChevronDown className="mr-1 h-4 w-4" />
              Why this match?
            </>
          )}
        </Button>
        <Button size="sm">View Details</Button>
      </CardFooter>
    </Card>
  );
}
