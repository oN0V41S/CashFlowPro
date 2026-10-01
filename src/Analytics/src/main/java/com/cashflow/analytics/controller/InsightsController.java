package com.cashflow.analytics.controller;

import com.cashflow.analytics.service.AnalyticsService;

import java.util.UUID;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController 
@RequestMapping("/api/insights")
public class InsightsController {
    
    public final AnalyticsService analyticsService;

    public InsightsController(AnalyticsService analyticsService) {
        this.analyticsService = analyticsService;
    }

    @GetMapping("/{accountId}")
    public Object getInsights(@PathVariable UUID accountId) {
        return analyticsService.getInsights(accountId);
    }
    
}
