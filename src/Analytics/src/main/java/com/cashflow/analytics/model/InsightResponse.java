package com.cashflow.analytics.model;

import java.time.Instant;
import java.util.UUID;

/**
 * InsightResponse
 */
public record InsightResponse(
    UUID accountId,
    String summary,
    String generatedAt
    ){}