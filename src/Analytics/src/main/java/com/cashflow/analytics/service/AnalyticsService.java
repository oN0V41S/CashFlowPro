package com.cashflow.analytics.service;

import com.cashflow.analytics.model.TransferEvent;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.UUID;

@Service
public class AnalyticsService {
    private static final Logger log = LoggerFactory.getLogger(AnalyticsService.class);

    private final CacheService cacheService;

    @Autowired
    public AnalyticsService(CacheService cacheService) {
        this.cacheService = cacheService;
    }

    /** 
     * Processes a transfer event:
     * 1. Updates source account aggregations
     * 2. Update target account aggregations
     * 3. Invalidate cache of both accounts
    */
    public void processTransfer(TransferEvent transferEvent){
        log.info("Processing transfer event: {} -> {} (R${})",
            transferEvent.fromAccountId(), transferEvent.toAccountId(), transferEvent.amount());
        
        // Invalidate Cache of involved accounts
        cacheService.invalidate(transferEvent.fromAccountId());
        cacheService.invalidate(transferEvent.toAccountId());

        log.info("Invalidated cache for accounts {} e {}", transferEvent.fromAccountId(), transferEvent.toAccountId());
    }
    
    // Get insights from an account (using cache)
    public Object getInsights(UUID accountId) {
        return cacheService.getOrCompute(accountId, () -> computeInsights(accountId));
    }

    // Compute reals insights (when not have cached)
    public Object computeInsights(UUID accountId) {
        log.info("Computing insights for account: {}", accountId);
        // Implement Aggregation logic (Marco 3)
        return null;
    } 
}
