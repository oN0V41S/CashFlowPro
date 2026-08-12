package com.cashflow.analytics.model;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.math.BigDecimal;
import java.util.UUID;

/* 
    * DTO representing a TransferCompleted event
    * Released for Core Banking (.NET) not RabbitMQ.
*/

public record TransferEvent(
    @JsonProperty("fromAccountId") UUID fromAccountId,
    @JsonProperty("toAccountId") UUID toAccountId,
    @JsonProperty("amount") BigDecimal amount
) {}