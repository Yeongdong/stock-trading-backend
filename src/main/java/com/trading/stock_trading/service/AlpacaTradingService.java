package com.trading.stock_trading.service;

import com.trading.stock_trading.entity.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.web.reactive.function.client.WebClient;
import reactor.core.publisher.Flux;
import reactor.core.publisher.Mono;

@Service
@RequiredArgsConstructor
@Slf4j
public class AlpacaTradingService {
    private final WebClient alpacaWebClient;

    public Mono<AccountInfo> getAccount() {
        return alpacaWebClient.get()
                .uri("/account")
                .retrieve()
                .bodyToMono(AccountInfo.class)
                .doOnSuccess(account -> log.info("계좌 정보 조회 성공: {}", account))
                .doOnError(error -> log.error("계좌 정보 조회 실패", error));
    }

    public Mono<OrderResponse> placeOrder(OrderRequest request) {
        return alpacaWebClient.post()
                .uri("/orders")
                .bodyValue(request)
                .retrieve()
                .bodyToMono(OrderResponse.class)
                .doOnSuccess(order -> log.info("주문 성공: {}", order))
                .doOnError(error -> log.error("주문 실패", error));
    }

    public Flux<Position> getPositions() {
        return alpacaWebClient.get()
                .uri("/positions")
                .retrieve()
                .bodyToFlux(Position.class)
                .doOnError(error -> log.error("포지션 조회 실패", error));
    }

    public Flux<Order> getOrders() {
        return alpacaWebClient.get()
                .uri("/orders")
                .retrieve()
                .bodyToFlux(Order.class)
                .doOnError(error -> log.error("주문 조회 실패", error));
    }
}